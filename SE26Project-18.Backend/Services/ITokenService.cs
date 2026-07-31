using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public interface ITokenService
{
    string GenerateAccessToken(long userId, string username);
    string GenerateAdminAccessToken(long adminId, string username);
    string GenerateRefreshToken();
    string HashToken(string token);
}
