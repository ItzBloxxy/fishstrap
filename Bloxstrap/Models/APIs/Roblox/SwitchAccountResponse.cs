namespace Bloxstrap.Models.APIs.Roblox
{
    public class SwitchAccountResponse
    {
        [JsonPropertyName("switched_from_user_id")]
        public string SwitchedFromUserId { get; set; } = String.Empty;

        [JsonPropertyName("switched_to_user_id")]
        public string SwitchedToUserId { get; set; } = String.Empty;
    }
}
