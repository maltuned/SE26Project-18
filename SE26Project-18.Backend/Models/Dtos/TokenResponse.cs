using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("access_token_expires_at")]
    public DateTime AccessTokenExpiresAt { get; set; }

    [JsonPropertyName("refresh_token_expires_at")]
    public DateTime RefreshTokenExpiresAt { get; set; }

    public TokenResponse() { }

    public TokenResponse(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
    }
}