using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class RecruitmentDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("publisher_id")]
    public long PublisherId { get; set; }

    [JsonPropertyName("game_id")]
    public long GameId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tags_id")]
    public long[] TagsId { get; set; } = [];

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("expired_at")]
    public string ExpiredAt { get; set; } = string.Empty;

    [JsonPropertyName("max_participants")]
    public int MaxParticipants { get; set; }

    [JsonPropertyName("current_participants")]
    public int CurrentParticipants { get; set; }
}
