using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

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

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == request.Username, ct);
        if (exists)
            throw new InvalidOperationException("Username already exists.");

        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Username, passwordHashed, UserRole.User);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHashed))
            throw new UnauthorizedAccessException("Invalid username or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);

        var storedToken = await _db
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHashed == tokenHashed, ct);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        storedToken.IsRevoked = true;

        await CleanupStaleTokensAsync(storedToken.Id, ct);

        var newTokens = await IssueTokensAsync(storedToken.User, ct);
        await _db.SaveChangesAsync(ct);

        return newTokens;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.TokenHashed == tokenHashed,
            ct
        );

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;
            await CleanupStaleTokensAsync(storedToken.UserId, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task CleanupStaleTokensAsync(long userId, CancellationToken ct)
    {
        await _db
            .RefreshTokens.Where(rt =>
                rt.UserId == userId && (rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow)
            )
            .ExecuteDeleteAsync(ct);
    }

    private async Task<TokenResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var refreshTokenDays = int.Parse(jwtSection["RefreshTokenExpiryDays"]!);
        var accessTokenMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"]!);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var hashedRefreshToken = _tokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken(
            user.Id,
            hashedRefreshToken,
            DateTime.UtcNow.AddDays(refreshTokenDays)
        );

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        return new TokenResponse(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            refreshTokenEntity.ExpiresAt
        );
    }
}
