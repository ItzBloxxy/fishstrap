using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public static class DockReveal
    {
        private static readonly Duration Slide = TimeSpan.FromMilliseconds(200);

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DockReveal), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject target) => (bool)target.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject target, bool value) => target.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Control control)
                return;

            control.MouseEnter -= Open;
            control.MouseLeave -= Close;

            if ((bool)e.NewValue)
            {
                control.MouseEnter += Open;
                control.MouseLeave += Close;
            }
        }

        private static void Open(object sender, MouseEventArgs e) => Animate((Control)sender, true);

        private static void Close(object sender, MouseEventArgs e) => Animate((Control)sender, false);

        private static void Animate(Control control, bool open)
        {
            if (control.Template?.FindName("Reveal", control) is not Decorator { Child: UIElement name } reveal)
                return;

            name.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            var ease = new CubicEase { EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn };

            reveal.BeginAnimation(FrameworkElement.WidthProperty,
                new DoubleAnimation(open ? name.DesiredSize.Width : 0, Slide) { EasingFunction = ease });

            reveal.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(open ? 1 : 0, Slide));
        }
    }
}
