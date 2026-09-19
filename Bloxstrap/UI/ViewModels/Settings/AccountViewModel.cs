using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Wpf.Ui.Common.Interfaces;

using Bloxstrap.RobloxInterfaces;
using Bloxstrap.UI.Utility;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class AccountViewModel : NotifyPropertyChangedViewModel, INavigationAware
    {
        private const int CollapsedGameCount = 3;

        private ScreenTimeData _data = new();

        private bool _showAllGames;

        private bool _loaded;

        public GenericTriState LoadState { get; private set; } = GenericTriState.Unknown;

        public bool IsBusy { get; private set; }

        public string Error { get; private set; } = String.Empty;

        #region Accounts

        public List<RobloxAccount> Accounts { get; private set; } = new();

        public bool HasAccounts => Accounts.Any();

        public ICommand SwitchCommand => new RelayCommand<RobloxAccount>(Switch);

        private async void Switch(RobloxAccount? account)
        {
            const string LOG_IDENT = "AccountViewModel::Switch";

            if (account is null || account.IsActive || IsBusy)
                return;

            SetBusy(true);

            try
            {
                await AccountSwitcher.SwitchAsync(account);

                _loaded = false;

                SetBusy(false);
                OnNavigatedTo();

                return;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to switch to {account.UserId}");
                App.Logger.WriteException(LOG_IDENT, ex);

                Error = ex.Message;
                OnPropertyChanged(nameof(Error));

                SetLoadState(GenericTriState.Failed);
            }
            finally
            {
                SetBusy(false);
            }
        }

        #endregion

        public bool CookieAccessDisabled => !App.Settings.Prop.AllowCookieAccess;

        public List<ScreenTimeDay> Days => _data.Days;

        public List<string> AxisLabels => _data.AxisLabels;

        public string AverageText => _data.AverageText;

        public double ChartHeight => ScreenTime.ChartHeight;

        public bool IsEmpty => LoadState == GenericTriState.Successful && _data.IsEmpty;

        public IEnumerable<ScreenTimeGame> Games => _showAllGames ? _data.Games : _data.Games.Take(CollapsedGameCount);

        public bool HasGames => _data.Games.Any();

        #region Breakdown

        public List<DonutSlice> Slices { get; private set; } = new();

        public double DonutSize => DonutChart.Size;

        public double DonutThickness => DonutChart.Thickness;

        public string BreakdownTotalText { get; private set; } = String.Empty;

        public bool HasBreakdown => Slices.Any();

        private void BuildBreakdown()
        {
            var entries = _data.Games
                .Select(x => (Label: x.Name.Length > 0 ? x.Name : x.UniverseId.ToString(),
                              Value: x.Duration.TotalMinutes,
                              ValueText: x.DurationText))
                .ToList();

            Slices = DonutChart.Build(
                entries,
                Strings.Menu_Playtime_Other,
                minutes => ScreenTimeData.FormatDuration(TimeSpan.FromMinutes(minutes)));

            BreakdownTotalText = ScreenTimeData.FormatDuration(
                TimeSpan.FromMinutes(entries.Sum(x => x.Value)));
        }

        #endregion

        public bool CanExpandGames => _data.Games.Count > CollapsedGameCount && !_showAllGames;

        public string ViewMoreText => Strings.Menu_Playtime_ViewMore;

        public ICommand ViewMoreCommand => new RelayCommand(ViewMore);

        public ICommand RefreshCommand => new RelayCommand(() => LoadData());

        public AccountViewModel() { }

        public void OnNavigatedTo()
        {
            if (_loaded)
                return;

            _loaded = true;

            LoadData();
        }

        public void OnNavigatedFrom() { }

        private void ViewMore()
        {
            _showAllGames = true;

            OnPropertyChanged(nameof(Games));
            OnPropertyChanged(nameof(CanExpandGames));
        }

        private async void LoadData()
        {
            const string LOG_IDENT = "AccountViewModel::LoadData";

            _showAllGames = false;

            if (CookieAccessDisabled)
            {
                OnPropertyChanged(nameof(CookieAccessDisabled));
                return;
            }

            SetBusy(true);
            SetLoadState(GenericTriState.Unknown);

            try
            {
                if (!App.Cookies.Loaded)
                    await Task.Run(App.Cookies.LoadCookies);

                Accounts = await AccountSwitcher.GetAccountsAsync();

                _data = await ScreenTime.FetchAsync();

                SetLoadState(GenericTriState.Successful);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load screen time");
                App.Logger.WriteException(LOG_IDENT, ex);

                Error = ex.Message;
                OnPropertyChanged(nameof(Error));

                SetLoadState(GenericTriState.Failed);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;

            OnPropertyChanged(nameof(IsBusy));
        }

        private void SetLoadState(GenericTriState state)
        {
            LoadState = state;

            BuildBreakdown();

            OnPropertyChanged(nameof(LoadState));
            OnPropertyChanged(nameof(Accounts));
            OnPropertyChanged(nameof(HasAccounts));
            OnPropertyChanged(nameof(Slices));
            OnPropertyChanged(nameof(BreakdownTotalText));
            OnPropertyChanged(nameof(HasBreakdown));
            OnPropertyChanged(nameof(Days));
            OnPropertyChanged(nameof(AxisLabels));
            OnPropertyChanged(nameof(AverageText));
            OnPropertyChanged(nameof(Games));
            OnPropertyChanged(nameof(HasGames));
            OnPropertyChanged(nameof(CanExpandGames));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
