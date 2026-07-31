using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class RecruitmentBriefDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("game")]
    public GameBriefDto? Game { get; set; }

    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = string.Empty;
}