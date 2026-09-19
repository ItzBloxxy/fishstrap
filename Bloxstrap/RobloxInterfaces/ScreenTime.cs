namespace Bloxstrap.RobloxInterfaces
{
    public static class ScreenTime
    {
        private const string ApiService = "apis";
        private const string ApiPath = "parental-controls-api/v1/parental-controls";

        public const double ChartHeight = 140;

        private const int AxisDivisions = 3;

        private const double MinimumBarHeight = 4;

        private static readonly int[] AxisSteps = { 5, 10, 15, 30, 60, 120, 180, 240, 300, 360, 480, 600, 720 };

        public static async Task<ScreenTimeData> FetchAsync()
        {
            const string LOG_IDENT = "ScreenTime::FetchAsync";

            var weekly = await Http.AuthGetJson<WeeklyScreenTimeResponse>(
                UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/get-weekly-screentime"));

            if (weekly is null)
                throw new InvalidHTTPResponseException("Deserialised WeeklyScreenTimeResponse is null");

            var data = new ScreenTimeData();

            BuildChart(data, weekly.DailyScreentimes.Select(x => (x.DaysAgo, x.MinutesPlayed)), weekly.LocalDayOfWeek);

            try
            {
                data.Games = await FetchTopGamesAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load top games");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return data;
        }

        private static void BuildChart(ScreenTimeData data, IEnumerable<(int DaysAgo, int Minutes)> source, int localDayOfWeek)
        {
            var days = source.OrderByDescending(x => x.DaysAgo).ToList();

            if (!days.Any())
                return;

            int maxMinutes = days.Max(x => x.Minutes);
            int step = AxisSteps.FirstOrDefault(x => x * AxisDivisions >= maxMinutes, AxisSteps[^1]);
            int axisMax = step * AxisDivisions;

            foreach (var day in days)
            {
                double height = axisMax == 0 ? 0 : (double)day.Minutes / axisMax * ChartHeight;

                if (day.Minutes > 0 && height < MinimumBarHeight)
                    height = MinimumBarHeight;

                data.Days.Add(new ScreenTimeDay
                {
                    Label = GetDayLabel(localDayOfWeek, day.DaysAgo),
                    Duration = TimeSpan.FromMinutes(day.Minutes),
                    BarHeight = height
                });
            }

            data.Average = TimeSpan.FromMinutes(days.Average(x => x.Minutes));

            for (int i = AxisDivisions; i >= 1; i--)
                data.AxisLabels.Add(ScreenTimeData.FormatDuration(TimeSpan.FromMinutes(step * i)));

            data.AxisLabels.Add("0");
        }

        private static string GetDayLabel(int localDayOfWeek, int daysAgo)
        {
            if (daysAgo == 0)
                return Strings.Menu_Playtime_Today;

            int index = ((localDayOfWeek - daysAgo) % 7 + 7) % 7;

            return Locale.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[index];
        }

        private static async Task<List<ScreenTimeGame>> FetchTopGamesAsync()
        {
            var response = await Http.AuthGetJson<TopScreenTimeResponse>(
                UrlBuilder.BuildApiUrl(ApiService, $"{ApiPath}/get-top-weekly-screentime-by-universe"));

            if (response is null)
                throw new InvalidHTTPResponseException("Deserialised TopScreenTimeResponse is null");

            var games = response.UniverseWeeklyScreentimes
                .Where(x => x.UniverseId != 0)
                .OrderByDescending(x => x.WeeklyMinutes)
                .Select(x => new ScreenTimeGame
                {
                    UniverseId = x.UniverseId,
                    Duration = TimeSpan.FromMinutes(x.WeeklyMinutes)
                })
                .ToList();

            await PopulateGameDetailsAsync(games);

            return games;
        }

        private static async Task PopulateGameDetailsAsync(List<ScreenTimeGame> games)
        {
            const string LOG_IDENT = "ScreenTime::PopulateGameDetailsAsync";

            if (!games.Any())
                return;

            string ids = String.Join(',', games.Select(x => x.UniverseId).Distinct());

            try
            {
                await UniverseDetails.FetchBulk(ids);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch universe details");
                App.Logger.WriteException(LOG_IDENT, ex);
                return;
            }

            foreach (var game in games)
            {
                var details = UniverseDetails.LoadFromCache(game.UniverseId);

                if (details is null)
                    continue;

                game.Name = details.Data.Name;
                game.Genre = details.Data.Genre;
                game.ThumbnailUrl = details.Thumbnail.ImageUrl;
            }
        }
    }
}
