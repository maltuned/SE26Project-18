using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public class GameService : IGameService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public GameService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<GameDto>> GetGamesAsync(string query = "")
    {
        var games = string.IsNullOrEmpty(query)
            ? await _db.Games.Include(g => g.Tags).ToListAsync()
            : await _db.Games.Include(g => g.Tags)
                .Where(g => g.Name.Contains(query)).ToListAsync();
        return games.Select(g => _mapper.ToGameDto(g)).ToList();
    }

    public async Task<GameDto?> GetGameByIdAsync(long id)
    {
        var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id);
        return game == null ? null : _mapper.ToGameDto(game);
    }

    public async Task<GameDto> CreateGameAsync(GameRequestDto request)
    {
        var tags = await _db.GameTags.Where(t => request.TagsId.Contains(t.Id)).ToListAsync();
        var game = new Game(request.Name)
        {
            Company = request.Company,
            Description = request.Description,
            Cover = request.Cover,
            Icon = request.Icon,
            Tags = tags,
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return _mapper.ToGameDto(game);
    }

    public async Task<GameDto> UpdateGameAsync(long id, GameRequestDto request)
    {
        var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException("游戏不存在");

        game.UpdateDetails(request.Name, request.Company, request.Description, request.Cover, request.Icon);

        var tags = await _db.GameTags.Where(t => request.TagsId.Contains(t.Id)).ToListAsync();
        game.UpdateTags(tags);

        await _db.SaveChangesAsync();
        return _mapper.ToGameDto(game);
    }

    public async Task<bool> DeleteGameAsync(long id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return false;
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return true;
    }
}