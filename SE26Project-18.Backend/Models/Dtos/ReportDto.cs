using System.Text.Json.Serialization;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Dtos;

public class ReportDto
{
    [JsonPropertyName("target_type")]
    public string TargetType { get; set; } = string.Empty;

    [JsonPropertyName("target_id")]
    public long TargetId { get; set; }

    [JsonPropertyName("violation_type")]
    public string ViolationType { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}