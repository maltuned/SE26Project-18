using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IGameService
{
    Task<List<GameDto>> GetGamesAsync(string query = "");
    Task<GameDto?> GetGameByIdAsync(long id);
    Task<GameDto> CreateGameAsync(GameRequestDto request);
    Task<GameDto> UpdateGameAsync(long id, GameRequestDto request);
    Task<bool> DeleteGameAsync(long id);
}
