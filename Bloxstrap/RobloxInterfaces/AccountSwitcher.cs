namespace Bloxstrap.RobloxInterfaces
{
    public static class AccountSwitcher
    {
        private const string ApiService = "apis";
        private const string ApiPath = "account-switcher/v1";

        public static async Task SwitchAsync(RobloxAccount account)
        {
            const string LOG_IDENT = "AccountSwitcher::SwitchAsync";

            if (account.IsActive)
                return;

            string csrf = await App.Cookies.GetXCSRF();

            var metadata = await FetchMetadataAsync(csrf);

            var payload = new SwitchAccountRequest
            {
                UserId = account.UserId.ToString(),
                EncryptedUsersDataBlob = metadata.EncryptedUsersDataBlob
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await App.Cookies.AuthPost(
                UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/switch"), content, csrf);

            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<SwitchAccountResponse>(await response.Content.ReadAsStringAsync());

            if (result is null || result.SwitchedToUserId != account.UserId.ToString())
                throw new InvalidHTTPResponseException(
                    $"Switch landed on '{result?.SwitchedToUserId}' rather than {account.UserId}");

            string cookie = ExtractAuthCookie(response)
                ?? throw new InvalidHTTPResponseException("Switch response carried no Set-Cookie header");

            App.Logger.WriteLine(LOG_IDENT, $"Switched to {account.UserId}, updating the cookie store");

            if (!App.Cookies.SetAuthCookie(cookie))
                throw new InvalidOperationException("Could not write the new session to Roblox's cookie store");
        }

        private static string? ExtractAuthCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var headers))
                return null;

            foreach (string header in headers)
            {
                var match = Regex.Match(header, @"\.ROBLOSECURITY=([^;]+)");

                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        private static async Task<LoggedInUsersResponse> FetchMetadataAsync(string csrf)
        {
            using var response = await App.Cookies.AuthPost(
                UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/getLoggedInUsersMetadata"), null, csrf);

            response.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<LoggedInUsersResponse>(await response.Content.ReadAsStringAsync());

            if (data is null)
                throw new InvalidHTTPResponseException("Deserialised LoggedInUsersResponse is null");

            return data;
        }

        public static async Task<List<RobloxAccount>> GetAccountsAsync()
        {
            var data = await FetchMetadataAsync(await App.Cookies.GetXCSRF());

            var accounts = data.Users
                .Where(x => !String.IsNullOrEmpty(x.UserId))
                .Select(x => new RobloxAccount
                {
                    UserId = long.TryParse(x.UserId, out long id) ? id : 0,
                    Username = x.Username,
                    DisplayName = x.DisplayName,
                    IsActive = x.UserId == data.ActiveUserId
                })
                .Where(x => x.UserId != 0)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Label)
                .ToList();

            await PopulateAvatarsAsync(accounts);

            return accounts;
        }

        private static async Task PopulateAvatarsAsync(List<RobloxAccount> accounts)
        {
            const string LOG_IDENT = "AccountSwitcher::PopulateAvatarsAsync";

            if (!accounts.Any())
                return;

            try
            {
                string ids = String.Join(',', accounts.Select(x => x.UserId));

                var response = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                    UrlBuilder.BuildApiUrl("thumbnails",
                        $"v1/users/avatar-headshot?userIds={ids}&size=48x48&format=Png&isCircular=true"));

                foreach (var account in accounts)
                    account.AvatarUrl = response.Data
                        .FirstOrDefault(x => x.TargetId == account.UserId)?.ImageUrl;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch avatars");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
