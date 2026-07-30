using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] AdminUserQueryRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _adminService.GetUsersAsync(request, ct));
    }

    [HttpGet("games")]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetGames(
        [FromQuery] AdminGameQueryRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _adminService.GetGamesAsync(request, ct));
    }

    [HttpGet("recruitments")]
    public async Task<ActionResult<PagedResponse<RecruitmentResponse>>> GetRecruitments(
        [FromQuery] AdminRecruitmentQueryRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _adminService.GetRecruitmentsAsync(request, ct));
    }
}
