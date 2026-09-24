using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bloxstrap.UI.Elements.Overlay
{
    public partial class OverlayToast : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const double Inset = 12;

        private static readonly TimeSpan Dwell = TimeSpan.FromSeconds(6);

        private readonly DispatcherTimer _timer;

        private HWND _hwnd;

        public IntPtr Handle => _hwnd;

        public OverlayToast()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = Dwell };
            _timer.Tick += (_, _) => Dismiss();
        }

        public void Present(string title, string message, Rect gameBounds)
        {
            ToastTitle.Text = title;
            ToastMessage.Text = message;

            _timer.Stop();

            BeginAnimation(OpacityProperty, null);

            Opacity = 0;

            Card.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            Width = Card.DesiredSize.Width;
            Height = Card.DesiredSize.Height;

            if (Visibility != Visibility.Visible)
                Show();

            UpdateLayout();

            Place(gameBounds);

            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));

            _timer.Start();
        }

        public void Dismiss()
        {
            _timer.Stop();

            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));

            fade.Completed += (_, _) =>
            {
                if (Opacity == 0)
                    Hide();
            };

            BeginAnimation(OpacityProperty, fade);
        }

        private void Place(Rect gameBounds)
        {
            Point bottomRight = new(gameBounds.Right, gameBounds.Bottom);

            if (PresentationSource.FromVisual(this)?.CompositionTarget is CompositionTarget target)
                bottomRight = target.TransformFromDevice.Transform(bottomRight);

            Left = bottomRight.X - ActualWidth - Inset;
            Top = bottomRight.Y - ActualHeight - Inset;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new HWND(new WindowInteropHelper(this).Handle);

            int exStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
                exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }
    }
}
