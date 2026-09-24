using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations;
using Bloxstrap.Models.Overlay;
using BadgesApi = Bloxstrap.RobloxInterfaces.Badges;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class BadgeTrackerViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher? _activityWatcher;

        private long _loadedUniverseId;

        public ObservableCollection<Badge> Badges { get; } = new();

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;

                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanRefresh));
            }
        }

        public bool CanRefresh => !IsBusy;

        public int EarnedCount => Badges.Count(x => x.Awarded);

        private bool ProgressKnown => Badges.Any(x => x.AwardedKnown);

        public double CompletionPercentage => Badges.Any() && ProgressKnown ? (double)EarnedCount / Badges.Count * 100 : 0;

        public string CompletionText
        {
            get
            {
                if (!Badges.Any())
                    return String.Empty;

                return ProgressKnown
                    ? String.Format(Strings.Menu_Overlay_Badges_Progress, EarnedCount, Badges.Count)
                    : String.Format(Strings.Menu_Overlay_Badges_ProgressUnknown, Badges.Count);
            }
        }

        public bool HasBadges => Badges.Any();

        public bool ShowEmptyState => !IsBusy && !Badges.Any();

        public string EmptyText => InGame
            ? Strings.Menu_Overlay_Badges_Empty
            : Strings.Menu_Overlay_Badges_NotInGame;

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.UniverseId != 0;

        public ICommand RefreshCommand => new RelayCommand(async () => await LoadAsync(true));

        public BadgeTrackerViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            if (_activityWatcher is null)
                return;

            _activityWatcher.OnGameJoin += async (_, _) => await App.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(Clear);
        }

        public async Task LoadAsync(bool force = false)
        {
            const string LOG_IDENT = "BadgeTrackerViewModel::LoadAsync";

            if (IsBusy)
                return;

            if (!InGame)
            {
                Clear();
                return;
            }

            long universeId = _activityWatcher!.Data.UniverseId;

            if (!force && universeId == _loadedUniverseId && Badges.Any())
                return;

            IsBusy = true;

            try
            {
                var badges = await BadgesApi.FetchAsync(universeId, _activityWatcher.Data.UserId);

                Badges.Clear();

                foreach (Badge badge in badges.OrderBy(x => x.Awarded).ThenByDescending(x => x.WinRatePercentage))
                    Badges.Add(badge);

                _loadedUniverseId = universeId;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load badges");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                IsBusy = false;

                Refreshed();
            }
        }

        private void Clear()
        {
            Badges.Clear();

            _loadedUniverseId = 0;

            Refreshed();
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(EarnedCount));
            OnPropertyChanged(nameof(CompletionPercentage));
            OnPropertyChanged(nameof(CompletionText));
            OnPropertyChanged(nameof(HasBadges));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
        }
    }
}
