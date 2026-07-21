using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    string HashToken(string token);
}