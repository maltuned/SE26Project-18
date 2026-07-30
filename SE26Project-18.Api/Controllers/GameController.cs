using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Media;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/games")]
public sealed class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    private readonly IMediaService _mediaService;

    public GameController(IGameService gameService, IMediaService mediaService)
    {
        _gameService = gameService;
        _mediaService = mediaService;
    }

    [HttpGet("{id:long}/icon")]
    [AllowAnonymous]
    public Task<IActionResult> GetIcon(long id, CancellationToken ct)
    {
        return GetMedia(id, GameMediaKind.Icon, ct);
    }

    [HttpGet("{id:long}/cover")]
    [AllowAnonymous]
    public Task<IActionResult> GetCover(long id, CancellationToken ct)
    {
        return GetMedia(id, GameMediaKind.Cover, ct);
    }

    [HttpPut("{id:long}/icon")]
    [Authorize(Policy = "RequireAdmin")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> PutIcon(long id, [FromForm] IFormFile file, CancellationToken ct)
    {
        return PutMedia(id, GameMediaKind.Icon, file, ct);
    }

    [HttpPut("{id:long}/cover")]
    [Authorize(Policy = "RequireAdmin")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> PutCover(long id, [FromForm] IFormFile file, CancellationToken ct)
    {
        return PutMedia(id, GameMediaKind.Cover, file, ct);
    }

    [HttpDelete("{id:long}/icon")]
    [Authorize(Policy = "RequireAdmin")]
    public Task<IActionResult> DeleteIcon(long id, CancellationToken ct)
    {
        return DeleteMedia(id, GameMediaKind.Icon, ct);
    }

    [HttpDelete("{id:long}/cover")]
    [Authorize(Policy = "RequireAdmin")]
    public Task<IActionResult> DeleteCover(long id, CancellationToken ct)
    {
        return DeleteMedia(id, GameMediaKind.Cover, ct);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] SearchGamesRequest request,
        CancellationToken ct
    )
    {
        var games = await _gameService.SearchAsync(request, ct);
        return Ok(games);
    }

    [HttpGet("{id:long}", Name = "GetGameById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var game = await _gameService.GetById(id, ct);
        return Ok(game ?? throw new NotFoundException("Game not found."));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(CreateGameRequest request, CancellationToken ct)
    {
        var game = await _gameService.CreateAsync(request, ct);
        return CreatedAtRoute("GetGameById", new { id = game.Id }, game);
    }

    [HttpPatch("{id:long}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(
        long id,
        UpdateGameRequest request,
        CancellationToken ct
    )
    {
        return Ok(await _gameService.UpdateAsync(id, request, ct));
    }

    private async Task<IActionResult> GetMedia(
        long id,
        GameMediaKind kind,
        CancellationToken ct
    )
    {
        var media = await _mediaService.OpenGameMediaAsync(id, kind, ct);
        if (media is null)
        {
            return NotFound();
        }

        return new FileStreamResult(media.Stream, "image/webp")
        {
            EntityTag = media.EntityTag,
            LastModified = media.LastModified,
            EnableRangeProcessing = false,
        };
    }

    private async Task<IActionResult> PutMedia(
        long id,
        GameMediaKind kind,
        IFormFile file,
        CancellationToken ct
    )
    {
        await _mediaService.StoreGameMediaAsync(id, kind, file, ct);
        return NoContent();
    }

    private async Task<IActionResult> DeleteMedia(
        long id,
        GameMediaKind kind,
        CancellationToken ct
    )
    {
        await _mediaService.DeleteGameMediaAsync(id, kind, ct);
        return NoContent();
    }
}
