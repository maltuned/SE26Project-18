using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class RecruitmentDetailDto : RecruitmentDto
{
    [JsonPropertyName("publisher")]
    public UserBriefDto Publisher { get; set; } = null!;

    [JsonPropertyName("game")]
    public GameBriefDto? Game { get; set; }

    [JsonPropertyName("gameTags")]
    public GameTagDto[] GameTags { get; set; } = [];

    [JsonPropertyName("recruitmentTags")]
    public RecruitmentTagDto[] RecruitmentTags { get; set; } = [];
}