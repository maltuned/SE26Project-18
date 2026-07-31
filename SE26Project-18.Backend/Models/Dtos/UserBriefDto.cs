using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class UserBriefDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}