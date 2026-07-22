namespace SE26Project_18.Api.Models.Responses;

public sealed class TokenResponse
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime AccessTokenExpiresAt { get; init; }

    public required DateTime RefreshTokenExpiresAt { get; init; }
}
