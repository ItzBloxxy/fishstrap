using Bloxstrap.RobloxInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bloxstrap
{
    public class CookiesManager
    {
        private CookieState _state = CookieState.Unknown;

        public EventHandler<CookieState>? StateChanged;
        public CookieState State {
            get => _state;
            set {
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }
        public bool Loaded => Enabled && State == CookieState.Success;
        private bool Enabled => App.Settings.Prop.AllowCookieAccess;

        private string AuthCookie = string.Empty;
        private const string AuthCookieName = ".ROBLOSECURITY";
        private const string SupportedVersion = "1";
        private const string AuthPattern = $@"\t{AuthCookieName}\t(.+?)(;|$)";
        private string CookiesPath => Path.Combine(Paths.Roblox, "LocalStorage", Deployment.IsDefaultRobloxDomain ? "RobloxCookies.dat" : $"{Deployment.RobloxDomain}_RobloxCookies.dat");

        public async Task<string> GetXCSRF()
        {
            Uri logoutUrl = UrlBuilder.BuildApiUrl("auth", "v2/logout");

            HttpResponseMessage response = await AuthPost(logoutUrl, null);

            response.Headers.TryGetValues("x-csrf-token", out IEnumerable<string>? values);

            if (values is null)
                throw new HttpRequestException("Failed to get x-csrf-token from response");

            return values.First();
        }

        public async Task<HttpResponseMessage> AuthRequest(HttpRequestMessage request, string csrf = "")
        {
            string? host = request.RequestUri?.Host;

            // basic host validation in case we accidentally send authenticated request somewhere unwanted
            if (host is null)
                throw new ArgumentNullException("Host cannot be null");

            if (
                !host.Equals(Deployment.RobloxDomain, StringComparison.OrdinalIgnoreCase) &&
                !host.EndsWith("." + Deployment.RobloxDomain, StringComparison.OrdinalIgnoreCase)
                )
                throw new HttpRequestException($"Host must end with Roblox domain ({Deployment.RobloxDomain})");

            if (!Enabled)
                throw new NullReferenceException("Cookie access is not enabled");

            if (!String.IsNullOrEmpty(csrf))
                request.Headers.Add("x-csrf-token", csrf);

            request.Headers.Add("Cookie", $".ROBLOSECURITY={AuthCookie}");
            var response = await App.HttpClient.SendAsync(request);

            return response;
        }

        public async Task<HttpResponseMessage> AuthGet(Uri? uri, string csrf = "") => await AuthRequest(new HttpRequestMessage { RequestUri = uri, Method = HttpMethod.Get }, csrf);
        public async Task<HttpResponseMessage> AuthPost(Uri? uri, HttpContent? content, string csrf = "") => await AuthRequest(new HttpRequestMessage { RequestUri = uri, Content = content, Method = HttpMethod.Post }, csrf);

        public async Task<AuthenticatedUser?> GetAuthenticated()
        {
            const string LOG_IDENT = "CookiesManager::GetAuthenticated";
            
            try
            {
                Uri apiUrl = UrlBuilder.BuildApiUrl("users", "v1/users/authenticated");
                HttpResponseMessage response = await AuthGet(apiUrl);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                AuthenticatedUser user = JsonSerializer.Deserialize<AuthenticatedUser>(content)!;

                return user;
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get authenticated user");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public bool SetAuthCookie(string cookie)
        {
            const string LOG_IDENT = "CookiesManager::SetAuthCookie";

            if (String.IsNullOrWhiteSpace(cookie))
                throw new ArgumentException("Refusing to write an empty auth cookie");

            if (Utilities.IsRobloxRunning())
            {
                App.Logger.WriteLine(LOG_IDENT, "Roblox is running, refusing to touch the cookie store");
                return false;
            }

            if (!File.Exists(CookiesPath))
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie file not found");
                return false;
            }

            string backupPath = CookiesPath + ".fishstrap-bak";
            string? tempPath = null;

            try
            {
                string original = File.ReadAllText(CookiesPath);
                var cookies = JsonSerializer.Deserialize<RobloxCookies>(original)!;

                byte[] plain = ProtectedData.Unprotect(
                    Convert.FromBase64String(cookies.Cookies), null, DataProtectionScope.CurrentUser);

                string jar = Encoding.UTF8.GetString(plain);

                if (!Regex.IsMatch(jar, AuthPattern))
                {
                    App.Logger.WriteLine(LOG_IDENT, "No auth cookie to replace");
                    return false;
                }

                int entriesBefore = jar.Split(';').Length;

                string updated = Regex.Replace(jar, AuthPattern, m => $"	{AuthCookieName}	{cookie}{m.Groups[2].Value}");

                if (updated.Split(';').Length != entriesBefore)
                    throw new InvalidOperationException("Cookie count changed during replacement");

                cookies.Cookies = Convert.ToBase64String(ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(updated), null, DataProtectionScope.CurrentUser));

                File.Copy(CookiesPath, backupPath, true);

                tempPath = CookiesPath + ".fishstrap-tmp";
                File.WriteAllText(tempPath, JsonSerializer.Serialize(cookies), new UTF8Encoding(false));

                Filesystem.AssertReadOnly(CookiesPath);
                File.Replace(tempPath, CookiesPath, null);
                tempPath = null;

                if (!VerifyWrite(cookie, entriesBefore))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Write did not verify, rolling back");
                    File.Copy(backupPath, CookiesPath, true);
                    return false;
                }

                AuthCookie = cookie;
                State = CookieState.Success;

                App.Logger.WriteLine(LOG_IDENT, "Auth cookie replaced");

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to replace the auth cookie");
                App.Logger.WriteException(LOG_IDENT, ex);

                try
                {
                    if (File.Exists(backupPath))
                        File.Copy(backupPath, CookiesPath, true);
                }
                catch (Exception restoreEx)
                {
                    App.Logger.WriteException(LOG_IDENT, restoreEx);
                }

                return false;
            }
            finally
            {
                foreach (string? leftover in new[] { tempPath, backupPath })
                {
                    if (leftover is null || !File.Exists(leftover))
                        continue;

                    try { File.Delete(leftover); }
                    catch (Exception ex) { App.Logger.WriteException(LOG_IDENT, ex); }
                }
            }
        }

        private bool VerifyWrite(string expected, int expectedEntries)
        {
            var written = JsonSerializer.Deserialize<RobloxCookies>(File.ReadAllText(CookiesPath))!;

            string jar = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                Convert.FromBase64String(written.Cookies), null, DataProtectionScope.CurrentUser));

            Match match = Regex.Match(jar, AuthPattern);

            return match.Success
                && match.Groups[1].Value == expected
                && jar.Split(';').Length == expectedEntries;
        }

        public async Task LoadCookies()
        {
            const string LOG_IDENT = "CookiesManager::LoadCookies";

            // we use the status to infrom user about it in the menu
            if (!Enabled)
            {
                State = CookieState.NotAllowed;
                App.Logger.WriteLine(LOG_IDENT, "Cookie access not allowed");
                return;
            }

            if (!string.IsNullOrEmpty(AuthCookie))
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie was already loaded!");
                return;
            }

            if (!File.Exists(CookiesPath))
            {
                State = CookieState.NotFound;
                App.Logger.WriteLine(LOG_IDENT, "Cookie file not found");
                return;
            }

            try
            {
                string content = File.ReadAllText(CookiesPath);
                var cookies = JsonSerializer.Deserialize<RobloxCookies>(content)!;

                if (cookies.Version != SupportedVersion)
                    App.Logger.WriteLine(LOG_IDENT, $"Unknown cookie version: {cookies.Version}");

                // here we got the raw bytes data which we have to decrypt with user scope
                // from that we get raw cookies data in roblox's format
                // in our case we will regex it since all we need is auth cookie
                byte[] encryptedData = Convert.FromBase64String(cookies.Cookies);
                byte[] unencryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);

                string rawCookies = Encoding.UTF8.GetString(unencryptedData);
                Match authCookieMatch = Regex.Match(rawCookies, AuthPattern);

                if (!authCookieMatch.Success)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Regex failed for cookies");
                    return;
                }

                string authCookie = authCookieMatch.Groups[1].Value;
                AuthCookie = authCookie; // could use better naming

                // we test the cookie to see if its valid
                AuthenticatedUser? user = await GetAuthenticated();
                if (user is null || user?.Id == 0)
                {
                    State = CookieState.Invalid;
                    App.Logger.WriteLine(LOG_IDENT, "Cookie is invalid");
                    return;
                }

                State = CookieState.Success;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load cookie!");
                App.Logger.WriteException(LOG_IDENT, ex); 

                State = CookieState.Failed;
            }

            return;
        }
    }
}
