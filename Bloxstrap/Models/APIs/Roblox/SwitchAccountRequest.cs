namespace Bloxstrap.Models.APIs.Roblox
{
    public class SwitchAccountRequest
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = String.Empty;

        [JsonPropertyName("encrypted_users_data_blob")]
        public string EncryptedUsersDataBlob { get; set; } = String.Empty;
    }
}
