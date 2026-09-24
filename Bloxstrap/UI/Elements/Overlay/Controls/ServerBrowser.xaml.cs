using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    /// <summary>
    /// Interaction logic for ServerBrowser.xaml
    /// </summary>
    public partial class ServerBrowser : UserControl
    {
        private ServerBrowserViewModel _viewModel;

        public ServerBrowser()
        {
            _viewModel = new ServerBrowserViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            _viewModel = new ServerBrowserViewModel(activityWatcher);

            DataContext = _viewModel;

            _ = _viewModel.InitialiseAsync();
        }
    }
}
