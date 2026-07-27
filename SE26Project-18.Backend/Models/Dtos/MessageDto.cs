using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class MessageDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("sender_id")]
    public long SenderId { get; set; }

    [JsonPropertyName("receiver_id")]
    public long ReceiverId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public UserBriefDto Sender { get; set; } = null!;

    [JsonPropertyName("receiver")]
    public UserBriefDto Receiver { get; set; } = null!;
}