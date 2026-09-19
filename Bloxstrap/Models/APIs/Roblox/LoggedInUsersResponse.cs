namespace Bloxstrap.Models.APIs.Roblox
{
    public class LoggedInUsersResponse
    {
        [JsonPropertyName("logged_in_users_metadata")]
        public List<LoggedInUser> Users { get; set; } = new();

        [JsonPropertyName("active_user_id")]
        public string ActiveUserId { get; set; } = String.Empty;

        [JsonPropertyName("encrypted_users_data_blob")]
        public string EncryptedUsersDataBlob { get; set; } = String.Empty;
    }

    public class LoggedInUser
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = String.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = String.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = String.Empty;
    }
}
