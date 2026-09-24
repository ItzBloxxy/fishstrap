using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.Models.Overlay;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    /// <summary>
    /// Interaction logic for FriendActivity.xaml
    /// </summary>
    public partial class FriendActivity : UserControl
    {
        private FriendActivityViewModel _viewModel;

        public FriendActivity()
        {
            _viewModel = new FriendActivityViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();
        }

        public void Attach(RobloxParty? party)
        {
            _viewModel = new FriendActivityViewModel(party);

            DataContext = _viewModel;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) => await _viewModel.LoadConversations();

        private async void ConversationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is FriendItem selected)
                await _viewModel.LoadConversationHistory(selected);
        }

        private async void MessageTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            await _viewModel.SendMessage();
        }

        private void CloseOverlay(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Hide();
    }
}
