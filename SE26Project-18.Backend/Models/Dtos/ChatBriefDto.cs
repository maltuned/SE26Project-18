using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class ChatBriefDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("other_user_avatar")]
    public string OtherUserAvatar { get; set; } = string.Empty;

    [JsonPropertyName("other_user_name")]
    public string OtherUserName { get; set; } = string.Empty;

    [JsonPropertyName("last_message_content")]
    public string LastMessageContent { get; set; } = string.Empty;

    [JsonPropertyName("last_message_at")]
    public string LastMessageAt { get; set; } = string.Empty;

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}