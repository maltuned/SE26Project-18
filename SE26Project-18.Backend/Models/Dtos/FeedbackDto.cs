using System.Text.Json.Serialization;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Dtos;

public class FeedbackDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}