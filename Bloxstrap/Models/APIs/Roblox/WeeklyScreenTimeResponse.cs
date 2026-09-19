namespace Bloxstrap.Models.APIs.Roblox
{
    public class WeeklyScreenTimeResponse
    {
        [JsonPropertyName("dailyScreentimes")]
        public List<DailyScreenTime> DailyScreentimes { get; set; } = new();

        [JsonPropertyName("localDayOfWeek")]
        public int LocalDayOfWeek { get; set; }
    }

    public class DailyScreenTime
    {
        [JsonPropertyName("daysAgo")]
        public int DaysAgo { get; set; }

        [JsonPropertyName("minutesPlayed")]
        public int MinutesPlayed { get; set; }
    }
}
