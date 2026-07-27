using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class GameBriefDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cover")]
    public string Cover { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}