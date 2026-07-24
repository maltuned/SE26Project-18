using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class RecruitmentDetailDto : RecruitmentDto
{
    [JsonPropertyName("publisher")]
    public UserDto Publisher { get; set; } = null!;

    [JsonPropertyName("game")]
    public GameDto Game { get; set; } = null!;

    [JsonPropertyName("gameTags")]
    public GameTagDto[] GameTags { get; set; } = [];

    [JsonPropertyName("recruitmentTags")]
    public RecruitmentTagDto[] RecruitmentTags { get; set; } = [];
}
