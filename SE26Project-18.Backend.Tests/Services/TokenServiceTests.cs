using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IConfigurationSection> _jwtSectionMock;

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _jwtSectionMock = new Mock<IConfigurationSection>();

        _jwtSectionMock.Setup(s => s["Secret"]).Returns("test-secret-key-at-least-32-chars-long!!!");
        _jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        _jwtSectionMock.Setup(s => s["Audience"]).Returns("TestAudience");
        _jwtSectionMock.Setup(s => s["AccessTokenExpiryMinutes"]).Returns("30");

        _configMock.Setup(c => c.GetSection("Jwt")).Returns(_jwtSectionMock.Object);

        _tokenService = new TokenService(_configMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt_WithCorrectClaims()
    {
        // Act
        var token = _tokenService.GenerateAccessToken(42, "testuser");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "TestAudience");
        Assert.Equal("42", jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "testuser");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_SetsCorrectExpiry()
    {
        // Act
        var token = _tokenService.GenerateAccessToken(1, "user");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert: should expire approximately 30 minutes from now
        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        Assert.True(jwt.ValidTo > expectedExpiry.AddMinutes(-2));
        Assert.True(jwt.ValidTo < expectedExpiry.AddMinutes(2));
    }

    [Fact]
    public void GenerateAdminAccessToken_IncludesAdminRoleClaim()
    {
        // Act
        var token = _tokenService.GenerateAdminAccessToken(1, "admin");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAdminAccessToken_ContainsCorrectSubject()
    {
        // Act
        var token = _tokenService.GenerateAdminAccessToken(99, "adminuser");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("99", jwt.Subject);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert: should be valid base64
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length); // 64 bytes encoded
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void HashToken_ReturnsConsistentHash()
    {
        // Arrange
        var input = "test-refresh-token";

        // Act
        var hash1 = _tokenService.HashToken(input);
        var hash2 = _tokenService.HashToken(input);

        // Assert: same input should produce same hash (deterministic)
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_ReturnsDifferentHashForDifferentInput()
    {
        // Act
        var hash1 = _tokenService.HashToken("token-abc");
        var hash2 = _tokenService.HashToken("token-xyz");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashToken_ReturnsBase64String()
    {
        // Act
        var hash = _tokenService.HashToken("some-token");

        // Assert: should be valid base64 (SHA256 produces 32 bytes -> 44 char base64)
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredSecret()
    {
        // Arrange
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns("different-secret-key-that-is-long-enough-123");
        jwtSection.Setup(s => s["Issuer"]).Returns("Issuer");
        jwtSection.Setup(s => s["Audience"]).Returns("Audience");
        jwtSection.Setup(s => s["AccessTokenExpiryMinutes"]).Returns("60");
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);
        var service = new TokenService(config.Object);

        // Act
        var token = service.GenerateAccessToken(1, "user");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(token);
        Assert.Equal("Issuer", jwt.Issuer);
    }
}
