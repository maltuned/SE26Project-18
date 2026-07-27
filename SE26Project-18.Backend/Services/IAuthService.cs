using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(string username, string password);
    Task<TokenResponse> LoginAsync(string username, string password);
    Task<TokenResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(long userId, string refreshToken);
}