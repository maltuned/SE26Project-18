using SE26Project_18.Backend.Data;
using Microsoft.Extensions.Configuration;
using Moq;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IConfigurationSection> _jwtSectionMock = new();

    private AppDbContext CreateDb() => TestDbContextFactory.Create();

    private AuthService CreateService(AppDbContext db)
    {
        _jwtSectionMock.Setup(s => s["AccessTokenExpiryMinutes"]).Returns("30");
        _jwtSectionMock.Setup(s => s["RefreshTokenExpiryDays"]).Returns("7");
        _configMock.Setup(c => c.GetSection("Jwt")).Returns(_jwtSectionMock.Object);

        _tokenMock.Setup(t => t.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()))
            .Returns<long, string>((id, name) => $"access-token-{id}");
        _tokenMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw-refresh-token");
        _tokenMock.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh-token");

        return new AuthService(db, _tokenMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task Register_CreatesUser_AndReturnsTokens()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var result = await service.RegisterAsync("newuser", "password123");

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(DateTime.MinValue, result.AccessTokenExpiresAt);
        var user = db.Users.First();
        Assert.Equal("newuser", user.Username);
        Assert.Equal("newuser", user.Nickname);
    }

    [Fact]
    public async Task Register_Throws_WhenUsernameExists()
    {
        var db = CreateDb();
        db.Users.Add(new User("existing", BCrypt.Net.BCrypt.HashPassword("pw")));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync("existing", "password"));
    }

    [Fact]
    public async Task Login_ReturnsTokens_WhenCredentialsValid()
    {
        var db = CreateDb();
        db.Users.Add(new User("user1", BCrypt.Net.BCrypt.HashPassword("pass")));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.LoginAsync("user1", "pass");

        Assert.NotNull(result.AccessToken);
        Assert.Contains("access-token", result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task Login_Throws_WhenUserDoesNotExist()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("missing", "password"));

        Assert.Equal("用户名或密码错误", exception.Message);
        _tokenMock.Verify(
            token => token.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_Throws_WhenWrongPassword()
    {
        var db = CreateDb();
        db.Users.Add(new User("user1", BCrypt.Net.BCrypt.HashPassword("correct")));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("user1", "wrongpass"));
    }

    [Fact]
    public async Task Login_Throws_WhenUserBanned()
    {
        var db = CreateDb();
        var user = new User("banned", BCrypt.Net.BCrypt.HashPassword("pw")) { Status = UserStatus.Banned };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("banned", "pw"));
    }

    [Fact]
    public async Task Login_Throws_WhenUserDeleted()
    {
        var db = CreateDb();
        var user = new User("deleted", BCrypt.Net.BCrypt.HashPassword("pw")) { Status = UserStatus.Deleted };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("deleted", "pw"));
    }

    [Fact]
    public async Task Refresh_Throws_WhenTokenInvalid()
    {
        var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync("invalid-token"));
    }

    [Fact]
    public async Task Refresh_Throws_WhenTokenIsRevoked()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(new RefreshToken(user.Id, "hashed-refresh-token", DateTime.UtcNow.AddDays(1))
        {
            IsRevoked = true,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync("raw-refresh-token"));

        Assert.Equal("无效或已过期的刷新令牌", exception.Message);
        _tokenMock.Verify(
            token => token.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Refresh_Throws_WhenTokenIsExpired()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(new RefreshToken(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddMinutes(-1)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync("raw-refresh-token"));

        Assert.Equal("无效或已过期的刷新令牌", exception.Message);
        _tokenMock.Verify(
            token => token.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.Deleted)]
    public async Task Refresh_Throws_WhenUserStatusDisallowsAuthentication(UserStatus status)
    {
        var db = CreateDb();
        var user = new User("user", "pw") { Status = status };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var storedToken = new RefreshToken(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1));
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync("raw-refresh-token"));

        Assert.Equal("账户已被禁用或注销", exception.Message);
        Assert.False(storedToken.IsRevoked);
        _tokenMock.Verify(
            token => token.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteDeleteAsync used by CleanupStaleTokensAsync")]
    public async Task Refresh_ReturnsNewTokens_WhenTokenValid()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var userId = db.Users.First().Id;
        db.RefreshTokens.Add(new RefreshToken(userId, "hashed-refresh-token", DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RefreshAsync("raw-refresh-token");

        Assert.NotNull(result.AccessToken);
        Assert.NotEqual("raw-refresh-token", result.RefreshToken);
        // Old token should be revoked
        Assert.True(db.RefreshTokens.OrderBy(t => t.Id).First().IsRevoked);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteDeleteAsync used by CleanupStaleTokensAsync")]
    public async Task Logout_RevokesToken()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var userId = db.Users.First().Id;
        db.RefreshTokens.Add(new RefreshToken(userId, "hashed-refresh-token", DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.LogoutAsync(userId, "raw-refresh-token");

        Assert.True(db.RefreshTokens.First().IsRevoked);
    }

    [Fact]
    public async Task Logout_NoOp_WhenTokenNotFound()
    {
        var db = CreateDb();
        var service = CreateService(db);

        // Should not throw
        await service.LogoutAsync(1, "nonexistent-token");
    }

    [Fact]
    public async Task Logout_NoOp_WhenTokenBelongsToAnotherUser()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var storedToken = new RefreshToken(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1));
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.LogoutAsync(user.Id + 1, "raw-refresh-token");

        Assert.False(storedToken.IsRevoked);
    }

    [Fact]
    public async Task Logout_NoOp_WhenTokenAlreadyRevoked()
    {
        var db = CreateDb();
        var user = new User("user", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var storedToken = new RefreshToken(
            user.Id,
            "hashed-refresh-token",
            DateTime.UtcNow.AddDays(1))
        {
            IsRevoked = true,
        };
        db.RefreshTokens.Add(storedToken);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.LogoutAsync(user.Id, "raw-refresh-token");

        Assert.True(storedToken.IsRevoked);
        Assert.Single(db.RefreshTokens);
    }

    [Fact]
    public async Task ChangePassword_Throws_WhenUserDoesNotExist()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(999, "old-password", "new-password"));

        Assert.Equal("用户不存在", exception.Message);
    }

    [Fact]
    public async Task ChangePassword_Throws_WhenOldPasswordIsWrong()
    {
        var db = CreateDb();
        var originalHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var user = new User("user", originalHash);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(user.Id, "wrong-password", "new-password"));

        Assert.Equal("原密码错误", exception.Message);
        Assert.Equal(originalHash, user.PasswordHashed);
    }

    [Fact]
    public async Task ChangePassword_Throws_WhenNewPasswordIsTooShort()
    {
        var db = CreateDb();
        var originalHash = BCrypt.Net.BCrypt.HashPassword("old-password");
        var user = new User("user", originalHash);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePasswordAsync(user.Id, "old-password", "short"));

        Assert.Equal("新密码长度不能少于 6 位", exception.Message);
        Assert.Equal(originalHash, user.PasswordHashed);
    }
}
