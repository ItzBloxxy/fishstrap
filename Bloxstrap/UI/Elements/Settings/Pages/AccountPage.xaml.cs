using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AccountPage.xaml
    /// </summary>
    public partial class AccountPage
    {
        public AccountPage()
        {
            DataContext = new AccountViewModel();

            InitializeComponent();
        }
    }
}
