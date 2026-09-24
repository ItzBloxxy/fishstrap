using Bloxstrap.Models.APIs.RobloxParty;

namespace Bloxstrap.Models.Overlay
{
    public class FriendItem
    {
        public string Username { get; set; } = String.Empty;

        public string StatusText { get; set; } = String.Empty;

        public string? ProfileImage { get; set; }

        public string ConversationId { get; set; } = String.Empty;

        public UserMessagesPage? Data { get; set; }
    }
}
