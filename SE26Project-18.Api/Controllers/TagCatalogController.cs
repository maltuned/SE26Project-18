using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
public sealed class GameTagController : ControllerBase
{
    [HttpGet("api/v1/game-tags")]
    public async Task<IActionResult> GetAll(
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => Ok(await service.GetGameTagsAsync(ct));

    [HttpPost("api/v1/game-tags")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(
        CreateTagRequest request,
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => StatusCode(StatusCodes.Status201Created, await service.CreateGameTagAsync(request, ct));
}

[ApiController]
[Authorize]
public sealed class UserTagController : ControllerBase
{
    [HttpGet("api/v1/user-tags")]
    public async Task<IActionResult> GetAll(
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => Ok(await service.GetUserTagsAsync(ct));

    [HttpPost("api/v1/user-tags")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(
        CreateTagRequest request,
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => StatusCode(StatusCodes.Status201Created, await service.CreateUserTagAsync(request, ct));
}

[ApiController]
[Authorize]
public sealed class RecruitmentTagController : ControllerBase
{
    [HttpGet("api/v1/recruitment-tags")]
    public async Task<IActionResult> GetAll(
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => Ok(await service.GetRecruitmentTagsAsync(ct));

    [HttpPost("api/v1/recruitment-tags")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(
        CreateTagRequest request,
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await service.CreateRecruitmentTagAsync(request, ct)
        );
}
