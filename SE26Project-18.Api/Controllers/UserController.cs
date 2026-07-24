using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        return Ok(
            await _userService.GetByIdAsync(GetCurrentUserId(), ct)
                ?? throw new NotFoundException("User not found.")
        );
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        return Ok(
            await _userService.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("User not found.")
        );
    }

    [HttpPatch("me")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var user = await _userService.UpdateAsync(GetCurrentUserId(), request, ct);
        return Ok(user);
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new AuthenticationException("Token does not contain a valid user identifier.");

        return userId;
    }
}
