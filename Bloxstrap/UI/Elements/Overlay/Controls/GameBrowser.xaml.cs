using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class GameBrowser : UserControl
    {
        private GameBrowserViewModel _viewModel;

        public GameBrowser()
        {
            _viewModel = new GameBrowserViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            _viewModel = new GameBrowserViewModel(activityWatcher);

            DataContext = _viewModel;
        }
    }
}
