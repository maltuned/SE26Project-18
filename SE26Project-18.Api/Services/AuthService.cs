using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class AuthService : IAuthService
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
            throw new ConflictException("Username already exists.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Username, passwordHashed, UserRole.User);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var tokens = IssueTokens(user);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return tokens;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHashed))
            throw new AuthenticationException("Invalid username or password.");

        if (user.Status == UserStatus.Suspended)
            throw new AuthenticationException("User account is suspended.");

        var tokens = IssueTokens(user);
        await _db.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var storedToken = await _db
            .RefreshTokens.Include(rt => rt.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHashed == tokenHashed, ct);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
            throw new AuthenticationException("Invalid or expired refresh token.");

        if (storedToken.User.Status == UserStatus.Suspended)
            throw new AuthenticationException("User account is suspended.");

        var consumed = await _db
            .RefreshTokens.Where(rt =>
                rt.Id == storedToken.Id && !rt.IsRevoked && rt.ExpiresAt > now
            )
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true), ct);
        if (consumed != 1)
            throw new AuthenticationException("Invalid or expired refresh token.");

        await CleanupStaleTokensAsync(storedToken.UserId, ct);
        var newTokens = IssueTokens(storedToken.User);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return newTokens;
    }

    public async Task LogoutAsync(long userId, string refreshToken, CancellationToken ct)
    {
        var tokenHashed = _tokenService.HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.TokenHashed == tokenHashed && rt.UserId == userId,
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

    private TokenResponse IssueTokens(User user)
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

        return new TokenResponse(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            refreshTokenEntity.ExpiresAt
        );
    }
}
