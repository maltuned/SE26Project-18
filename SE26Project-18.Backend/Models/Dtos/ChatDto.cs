using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class ChatDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("recruitment_id")]
    public long RecruitmentId { get; set; }

    [JsonPropertyName("recruitment_title")]
    public string RecruitmentTitle { get; set; } = string.Empty;

    [JsonPropertyName("other_user")]
    public UserDto OtherUser { get; set; } = null!;

    [JsonPropertyName("last_message")]
    public MessageDto? LastMessage { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("chat_status")]
    public string ChatStatus { get; set; } = string.Empty;

    [JsonPropertyName("new_message_at")]
    public string NewMessageAt { get; set; } = string.Empty;

    [JsonPropertyName("users")]
    public ChatUserDto[]? Users { get; set; }

    [JsonPropertyName("recruitment")]
    public RecruitmentDto? Recruitment { get; set; }
}
