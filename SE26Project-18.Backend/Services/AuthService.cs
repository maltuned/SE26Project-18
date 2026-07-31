using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, ITokenService tokenService, IConfiguration configuration)
    {
        _db = db;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponse> RegisterAsync(string username, string password)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
            throw new InvalidOperationException("用户名已存在");

        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User(username, passwordHashed)
        {
            Nickname = username,
            Settings = new UserSettings
            {
                PushEnabled = true,
                ProfileVisible = true,
                DarkMode = false,
            },
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(user.Id, user.Username);
    }

    public async Task<TokenResponse> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHashed))
            throw new InvalidOperationException("用户名或密码错误");

        if (user.Status == UserStatus.Banned || user.Status == UserStatus.Deleted)
            throw new InvalidOperationException("账户已被禁用或注销");

        return await IssueTokensAsync(user.Id, user.Username);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);
        var now = DateTime.UtcNow;

        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHashed == tokenHashed);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
            throw new InvalidOperationException("无效或已过期的刷新令牌");

        if (storedToken.User.Status == UserStatus.Banned || storedToken.User.Status == UserStatus.Deleted)
            throw new InvalidOperationException("账户已被禁用或注销");

        storedToken.IsRevoked = true;
        await CleanupStaleTokensAsync(storedToken.UserId);
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(storedToken.UserId, storedToken.User.Username);
    }

    public async Task LogoutAsync(long userId, string refreshToken)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.TokenHashed == tokenHashed && rt.UserId == userId);

        if (storedToken != null && !storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;
            await CleanupStaleTokensAsync(userId);
            await _db.SaveChangesAsync();
        }
    }

    public async Task ChangePasswordAsync(long userId, string oldPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("用户不存在");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHashed))
            throw new InvalidOperationException("原密码错误");

        if (newPassword.Length < 6)
            throw new InvalidOperationException("新密码长度不能少于 6 位");

        user.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

        await _db.SaveChangesAsync();
    }

    private async Task CleanupStaleTokensAsync(long userId)
    {
        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && (rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow))
            .ExecuteDeleteAsync();
    }

    private async Task<TokenResponse> IssueTokensAsync(long userId, string username)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var refreshTokenDays = int.Parse(jwtSection["RefreshTokenExpiryDays"]!);
        var accessTokenMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"]!);

        var accessToken = _tokenService.GenerateAccessToken(userId, username);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken(
            userId,
            hashedRefreshToken,
            DateTime.UtcNow.AddDays(refreshTokenDays)
        );

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        return new TokenResponse(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            refreshTokenEntity.ExpiresAt
        );
    }
}