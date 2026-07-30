using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<IUserService> _userMock = new();

    private AuthController CreateController(long? userId = 1)
    {
        var controller = new AuthController(_authMock.Object, _userMock.Object);
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return controller;
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenSuccessful()
    {
        var response = new TokenResponse("at", "rt", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        _authMock.Setup(a => a.RegisterAsync("user", "pass")).ReturnsAsync(response);
        var controller = CreateController(null);

        var result = await controller.Register(new AuthRequest { Username = "user", Password = "pass" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenUserExists()
    {
        _authMock.Setup(a => a.RegisterAsync("existing", "pass"))
            .ThrowsAsync(new InvalidOperationException("用户名已存在"));
        var controller = CreateController(null);

        var result = await controller.Register(new AuthRequest { Username = "existing", Password = "pass" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<TokenResponse>>(ok.Value);
        Assert.Equal(409, apiResp.Status);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenSuccessful()
    {
        var response = new TokenResponse("at", "rt", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        _authMock.Setup(a => a.LoginAsync("user", "pass")).ReturnsAsync(response);
        var controller = CreateController(null);

        var result = await controller.Login(new AuthRequest { Username = "user", Password = "pass" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalid()
    {
        _authMock.Setup(a => a.LoginAsync("bad", "cred"))
            .ThrowsAsync(new InvalidOperationException("用户名或密码错误"));
        var controller = CreateController(null);

        var result = await controller.Login(new AuthRequest { Username = "bad", Password = "cred" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<TokenResponse>>(ok.Value);
        Assert.Equal(401, apiResp.Status);
    }

    [Fact]
    public async Task Refresh_ReturnsOk_WhenValid()
    {
        var response = new TokenResponse("new-at", "new-rt", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        _authMock.Setup(a => a.RefreshAsync("valid-rt")).ReturnsAsync(response);
        var controller = CreateController(null);

        var result = await controller.Refresh(new RefreshRequest { RefreshToken = "valid-rt" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Me_ReturnsUser_WhenAuthenticated()
    {
        var userDto = new UserDto { Id = 1, Username = "test", Nickname = "Test" };
        _userMock.Setup(u => u.GetUserByIdAsync(1)).ReturnsAsync(userDto);
        var controller = CreateController(1);

        var result = await controller.Me();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WhenNoUserId()
    {
        var controller = CreateController(null);

        var result = await controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<UserDto>>(ok.Value);
        Assert.Equal(401, apiResp.Status);
    }

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        var controller = CreateController(1);

        var result = await controller.Logout(new RefreshRequest { RefreshToken = "some-token" });

        Assert.IsType<OkObjectResult>(result.Result);
        _authMock.Verify(a => a.LogoutAsync(1, "some-token"), Times.Once);
    }
}
