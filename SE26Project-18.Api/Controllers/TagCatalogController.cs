using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
public sealed class GameTagController : ControllerBase
{
    [HttpPost("api/v1/game-tags")]
    public async Task<IActionResult> Create(
        CreateTagRequest request,
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => StatusCode(StatusCodes.Status201Created, await service.CreateGameTagAsync(request, ct));
}

[ApiController]
[Authorize(Policy = "RequireAdmin")]
public sealed class UserTagController : ControllerBase
{
    [HttpPost("api/v1/user-tags")]
    public async Task<IActionResult> Create(
        CreateTagRequest request,
        [FromServices] ITagCatalogService service,
        CancellationToken ct
    ) => StatusCode(StatusCodes.Status201Created, await service.CreateUserTagAsync(request, ct));
}

[ApiController]
[Authorize(Policy = "RequireAdmin")]
public sealed class RecruitmentTagController : ControllerBase
{
    [HttpPost("api/v1/recruitment-tags")]
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
