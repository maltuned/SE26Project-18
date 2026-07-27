using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class ResponseDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("recruitment_id")]
    public long RecruitmentId { get; set; }

    [JsonPropertyName("responser_id")]
    public long ResponserId { get; set; }

    [JsonPropertyName("response_status")]
    public string ResponseStatus { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("responser")]
    public UserBriefDto Responser { get; set; } = null!;
}