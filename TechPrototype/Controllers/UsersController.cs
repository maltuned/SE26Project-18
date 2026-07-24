using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.LoginAsync(request.Username, request.Password);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户名或密码错误", 401));
        return Ok(ApiResponse<UserDto>.Success(user, "登录成功"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.RegisterAsync(request.Username, request.Password);
            return Ok(ApiResponse<UserDto>.Success(user, "注册成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<UserDto>.Fail(ex.Message, 409));
        }
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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(ApiResponse<List<UserDto>>.Success(users));
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public long Id { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
}
