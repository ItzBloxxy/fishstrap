namespace Bloxstrap.Models.APIs.Roblox
{
    public class UserSettingsResponse
    {
        [JsonPropertyName("whoCanSeeMyOnlineStatus")]
        public UserSettingValue? OnlineStatus { get; set; }

        [JsonPropertyName("whoCanJoinMeInExperiences")]
        public UserSettingValue? JoinStatus { get; set; }
    }

    public class UserSettingValue
    {
        [JsonPropertyName("currentValue")]
        public string? CurrentValue { get; set; }
    }
}
