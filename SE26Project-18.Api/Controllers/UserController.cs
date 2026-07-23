using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET /api/v1/User/me — 获取当前登录用户信息（从 JWT sub 提取 ID）
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (subClaim is null || !long.TryParse(subClaim, out var userId))
            return Unauthorized();

        var user = await _userService.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var user = await _userService.UpdateAsync(id, request, ct);
        return Ok(user);
    }
}
