using System.Windows;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.UI.Elements.Overlay;

namespace Bloxstrap.Integrations
{
    public class Overlay : IDisposable
    {
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        public event EventHandler<OverlayBounds>? BoundsChanged;
        public event EventHandler<bool>? GameVisibilityChanged;
        public event EventHandler? WindowClosed;

        public readonly RealtimeMessaging Messaging = new();

        public readonly ActivityWatcher? ActivityWatcher;

        private readonly HWND _robloxWindow;
        private readonly uint _robloxProcessId;

        private WINEVENTPROC? _systemCallback;
        private WINEVENTPROC? _objectCallback;

        private UnhookWinEventSafeHandle? _systemHook;
        private UnhookWinEventSafeHandle? _objectHook;

        private GameOverlay? _window;
        private OverlayToast? _toast;
        private OverlayBounds? _lastBounds;
        private bool _disposed;

        public Overlay(long windowHandle, long robloxProcessId, ActivityWatcher? activityWatcher)
        {
            _robloxWindow = (HWND)(IntPtr)windowHandle;
            _robloxProcessId = (uint)robloxProcessId;

            ActivityWatcher = activityWatcher;
        }

        public void Start()
        {
            const string LOG_IDENT = "Overlay::Start";

            if (_robloxWindow == IntPtr.Zero)
            {
                App.Logger.WriteLine(LOG_IDENT, "No window handle, not starting");
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Attaching to window {(IntPtr)_robloxWindow}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                _window = new GameOverlay(this);
                _window.PrepareHidden();

                _systemCallback = new WINEVENTPROC(OnSystemEvent);
                _objectCallback = new WINEVENTPROC(OnObjectEvent);

                _systemHook = PInvoke.SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_MINIMIZEEND,
                    null, _systemCallback, _robloxProcessId, 0, WINEVENT_OUTOFCONTEXT);

                _objectHook = PInvoke.SetWinEventHook(
                    EVENT_OBJECT_DESTROY, EVENT_OBJECT_LOCATIONCHANGE,
                    null, _objectCallback, _robloxProcessId, 0, WINEVENT_OUTOFCONTEXT);
            });

            SyncBounds();

            Messaging.ConnectToUserhub();
        }

        public void SyncBounds()
        {
            OverlayBounds bounds = GetBounds();

            _lastBounds = bounds;

            Application.Current.Dispatcher.Invoke(() => BoundsChanged?.Invoke(this, bounds));
        }

        public void AnchorAboveGame(IntPtr overlayHandle)
        {
            if (overlayHandle == IntPtr.Zero || _robloxWindow == IntPtr.Zero)
                return;

            PInvoke.SetWindowPos(
                (HWND)overlayHandle,
                HWND.Null,
                0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }

        public bool ShowToast(string title, string message)
        {
            const string LOG_IDENT = "Overlay::ShowToast";

            if (_disposed || _window is null || _robloxWindow == IntPtr.Zero)
            {
                App.Logger.WriteLine(LOG_IDENT, "No overlay to show it in");
                return false;
            }

            if (IsGameMinimised() || !IsGameForeground())
            {
                App.Logger.WriteLine(LOG_IDENT, "Game isn't in front, leaving it to the desktop notification");
                return false;
            }

            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    OverlayBounds bounds = GetBounds();

                    if (bounds.Rect.Width <= 0 || bounds.Rect.Height <= 0)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Could not measure the game window");
                        return false;
                    }

                    App.Logger.WriteLine(LOG_IDENT, $"{title}: {message.Replace("\n", "\\n")}");

                    _toast ??= new OverlayToast();
                    _toast.Present(title, message, bounds.Rect);

                    return true;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to show a toast");
                    App.Logger.WriteException(LOG_IDENT, ex);

                    return false;
                }
            });
        }

        public void DismissToast() => _toast?.Dismiss();

        public bool IsGameMinimised() => PInvoke.IsIconic(_robloxWindow);

        public bool IsGameForeground() => PInvoke.GetForegroundWindow() == _robloxWindow;

        public void FocusGame() => PInvoke.SetForegroundWindow(_robloxWindow);

        private OverlayBounds GetBounds()
        {
            if (!PInvoke.GetClientRect(_robloxWindow, out RECT client))
                return new OverlayBounds(Rect.Empty, false);

            var origin = new System.Drawing.Point(client.left, client.top);

            if (!PInvoke.ClientToScreen(_robloxWindow, ref origin))
                return new OverlayBounds(Rect.Empty, false);

            var bounds = new Rect(
                origin.X,
                origin.Y,
                Math.Max(client.right - client.left, 0),
                Math.Max(client.bottom - client.top, 0));

            return new OverlayBounds(bounds, PInvoke.IsZoomed(_robloxWindow));
        }

        private void OnSystemEvent(HWINEVENTHOOK hook, uint iEvent, HWND hWnd, int idObject, int idChild, uint thread, uint time)
        {
            if (hWnd != _robloxWindow)
                return;

            switch (iEvent)
            {
                case EVENT_SYSTEM_MINIMIZESTART:
                    Application.Current.Dispatcher.Invoke(() => GameVisibilityChanged?.Invoke(this, false));
                    break;

                case EVENT_SYSTEM_MINIMIZEEND:
                    Application.Current.Dispatcher.Invoke(() => GameVisibilityChanged?.Invoke(this, true));
                    SyncBounds();
                    break;

                case EVENT_SYSTEM_FOREGROUND:
                    Application.Current.Dispatcher.Invoke(() => _window?.Reanchor());
                    break;
            }
        }

        private void OnObjectEvent(HWINEVENTHOOK hook, uint iEvent, HWND hWnd, int idObject, int idChild, uint thread, uint time)
        {
            const string LOG_IDENT = "Overlay::OnObjectEvent";

            if (hWnd != _robloxWindow)
                return;

            if (iEvent == EVENT_OBJECT_DESTROY)
            {
                App.Logger.WriteLine(LOG_IDENT, "Game window went away");

                Application.Current.Dispatcher.Invoke(() => WindowClosed?.Invoke(this, EventArgs.Empty));
                return;
            }

            try
            {
                PublishBounds();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to track the game window");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void PublishBounds()
        {
            OverlayBounds bounds = GetBounds();

            if (bounds == _lastBounds)
                return;

            _lastBounds = bounds;

            BoundsChanged?.Invoke(this, bounds);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _systemHook?.Dispose();
            _objectHook?.Dispose();

            _systemHook = null;
            _objectHook = null;

            _systemCallback = null;
            _objectCallback = null;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _toast?.Close();
                _window?.Close();
            });

            _ = Messaging.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }
}
