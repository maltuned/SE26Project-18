using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class UserSettingsDto
{
    [JsonPropertyName("push_enabled")]
    public bool PushEnabled { get; set; }

    [JsonPropertyName("profile_visible")]
    public bool ProfileVisible { get; set; }

    [JsonPropertyName("dark_mode")]
    public bool DarkMode { get; set; }
}