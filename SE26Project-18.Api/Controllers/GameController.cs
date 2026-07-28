using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/games")]
public sealed class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
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
}
