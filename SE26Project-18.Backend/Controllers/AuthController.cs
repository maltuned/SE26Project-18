using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<UserDto>.Fail("无效的令牌", 401));

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));

        return Ok(ApiResponse<UserDto>.Success(user));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Register([FromBody] AuthRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Username, request.Password);
            return Ok(ApiResponse<TokenResponse>.Success(result, "注册成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<TokenResponse>.Fail(ex.Message, 409));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login([FromBody] AuthRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);
            return Ok(ApiResponse<TokenResponse>.Success(result, "登录成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<TokenResponse>.Fail(ex.Message, 401));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<TokenResponse>.Fail(ex.Message, 401));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] RefreshRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<bool>.Fail("无效的令牌", 401));

        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(ApiResponse<bool>.Success(true, "已登出"));
    }
}

public class AuthRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}