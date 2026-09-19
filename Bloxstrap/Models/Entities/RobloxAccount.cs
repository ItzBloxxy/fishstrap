namespace Bloxstrap.Models.Entities
{
    public class RobloxAccount
    {
        public long UserId { get; set; }

        public string Username { get; set; } = String.Empty;

        public string DisplayName { get; set; } = String.Empty;

        public bool IsActive { get; set; }

        public string? AvatarUrl { get; set; }

        public string UsernameText => $"@{Username}";

        public string Label
        {
            get
            {
                if (!String.IsNullOrEmpty(DisplayName))
                    return DisplayName;

                return String.IsNullOrEmpty(Username) ? UserId.ToString() : Username;
            }
        }
    }
}
