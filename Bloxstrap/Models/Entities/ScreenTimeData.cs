namespace Bloxstrap.Models.Entities
{
    public class ScreenTimeDay
    {
        public DateTime Date { get; set; }

        public string Label { get; set; } = String.Empty;

        public TimeSpan Duration { get; set; }

        public string DurationText => ScreenTimeData.FormatDuration(Duration);

        public double BarHeight { get; set; }
    }

    public class ScreenTimeGame
    {
        public long UniverseId { get; set; }

        public TimeSpan Duration { get; set; }

        public string DurationText => ScreenTimeData.FormatDuration(Duration);

        public string Name { get; set; } = String.Empty;

        public string Genre { get; set; } = String.Empty;

        public string? ThumbnailUrl { get; set; }
    }

    public class ScreenTimeData
    {
        public List<ScreenTimeDay> Days { get; set; } = new();

        public List<ScreenTimeGame> Games { get; set; } = new();

        public TimeSpan Average { get; set; }

        public string AverageText => FormatDuration(Average);

        public List<string> AxisLabels { get; set; } = new();

        public bool IsEmpty => !Days.Any(x => x.Duration > TimeSpan.Zero) && !Games.Any();

        public static string FormatDuration(TimeSpan duration)
        {
            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;

            if (hours == 0)
                return $"{minutes}m";

            if (minutes == 0)
                return $"{hours}h";

            return $"{hours}h {minutes}m";
        }
    }
}
