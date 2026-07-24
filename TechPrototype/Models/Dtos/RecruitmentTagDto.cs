using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class RecruitmentTagDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
