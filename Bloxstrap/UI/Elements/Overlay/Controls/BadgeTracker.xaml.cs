using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    /// <summary>
    /// Interaction logic for BadgeTracker.xaml
    /// </summary>
    public partial class BadgeTracker : UserControl
    {
        private BadgeTrackerViewModel _viewModel;

        public BadgeTracker()
        {
            _viewModel = new BadgeTrackerViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            _viewModel = new BadgeTrackerViewModel(activityWatcher);

            DataContext = _viewModel;

            _ = _viewModel.LoadAsync();
        }
    }
}
