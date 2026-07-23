using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct
    )
    {
        var response = await _authService.RegisterAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct
    )
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct
    )
    {
        await _authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
