namespace Bloxstrap.Models.APIs.Roblox
{
    public class TopScreenTimeResponse
    {
        [JsonPropertyName("universeWeeklyScreentimes")]
        public List<UniverseWeeklyScreenTime> UniverseWeeklyScreentimes { get; set; } = new();
    }

    public class UniverseWeeklyScreenTime
    {
        [JsonPropertyName("universeId")]
        public long UniverseId { get; set; }

        [JsonPropertyName("weeklyMinutes")]
        public int WeeklyMinutes { get; set; }
    }
}
