using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetGames([FromQuery] string query = "")
    {
        var games = await _gameService.GetGamesAsync(query);
        return Ok(ApiResponse<List<GameDto>>.Success(games));
    }

    [HttpGet("by-id")]
    public async Task<ActionResult<ApiResponse<GameDto>>> GetGameById([FromQuery] long id)
    {
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
            return Ok(ApiResponse<GameDto>.Fail("游戏不存在", 404));
        return Ok(ApiResponse<GameDto>.Success(game));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GameDto>>> CreateGame([FromBody] GameRequestDto request)
    {
        var game = await _gameService.CreateGameAsync(request);
        return Ok(ApiResponse<GameDto>.Success(game));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteGame(long id)
    {
        var result = await _gameService.DeleteGameAsync(id);
        return Ok(ApiResponse<bool>.Success(result, result ? "删除成功" : "游戏不存在"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<GameDto>>> UpdateGame(long id, [FromBody] GameRequestDto request)
    {
        try
        {
            var game = await _gameService.UpdateGameAsync(id, request);
            return Ok(ApiResponse<GameDto>.Success(game));
        }
        catch (KeyNotFoundException)
        {
            return Ok(ApiResponse<GameDto>.Fail("游戏不存在", 404));
        }
    }
}