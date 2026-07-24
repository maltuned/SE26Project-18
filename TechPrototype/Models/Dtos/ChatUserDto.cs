using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class ChatUserDto
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("sent_message")]
    public bool SentMessage { get; set; }
}
