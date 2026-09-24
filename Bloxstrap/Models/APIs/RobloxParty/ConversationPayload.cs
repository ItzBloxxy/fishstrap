namespace Bloxstrap.Models.APIs.RobloxParty
{
    public class ConversationPayload
    {
        [JsonPropertyName("conversation_id")]
        public string Id { get; set; } = String.Empty;
    }
}
