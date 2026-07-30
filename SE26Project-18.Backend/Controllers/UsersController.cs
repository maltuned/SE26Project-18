using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("update")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(request.Id, request.Data);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));
        return Ok(ApiResponse<UserDto>.Success(user, "更新成功"));
    }

    [HttpGet("by-id")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById([FromQuery] long id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));
        return Ok(ApiResponse<UserDto>.Success(user));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserProfile([FromQuery] long id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        long.TryParse(userIdClaim, out var requesterId);

        var (user, isPrivate) = await _userService.GetUserProfileAsync(requesterId, id);

        if (isPrivate)
            return Ok(new ApiResponse<UserDto>(403, user, "该用户未公开个人空间"));

        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));

        return Ok(ApiResponse<UserDto>.Success(user));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(ApiResponse<List<UserDto>>.Success(users));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetSettings()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<UserSettingsDto>.Fail("无效的令牌", 401));

        var settings = await _userService.GetUserSettingsAsync(userId);
        if (settings == null)
            return Ok(ApiResponse<UserSettingsDto>.Fail("设置不存在", 404));
        return Ok(ApiResponse<UserSettingsDto>.Success(settings));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateSettings([FromBody] UserSettingsDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<UserSettingsDto>.Fail("无效的令牌", 401));

        var settings = await _userService.UpdateUserSettingsAsync(userId, request);
        if (settings == null)
            return Ok(ApiResponse<UserSettingsDto>.Fail("设置不存在", 404));
        return Ok(ApiResponse<UserSettingsDto>.Success(settings, "设置已更新"));
    }
}

public class UpdateUserRequest
{
    public long Id { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
}