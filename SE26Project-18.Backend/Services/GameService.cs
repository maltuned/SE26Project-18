using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Services;

public class GameService : IGameService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;
    private readonly IEmbeddingSyncScheduler _embeddingSync;

    public GameService(AppDbContext db, MapperService mapper, IEmbeddingSyncScheduler embeddingSync)
    {
        _db = db;
        _mapper = mapper;
        _embeddingSync = embeddingSync;
    }

    public async Task<List<GameDto>> GetGamesAsync(string query = "")
    {
        if (string.IsNullOrEmpty(query))
            return (await _db.Games.Include(g => g.Tags).ToListAsync())
                .Select(g => _mapper.ToGameDto(g)).ToList();

        if (long.TryParse(query, out var id))
        {
            var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id);
            return game == null ? [] : [_mapper.ToGameDto(game)];
        }

        var games = await _db.Games.Include(g => g.Tags)
            .Where(g => g.Name.Contains(query) || g.NameEn.Contains(query) || g.Aliases.Contains(query))
            .ToListAsync();
        return games.Select(g => _mapper.ToGameDto(g)).ToList();
    }

    public async Task<GameDto?> GetGameByIdAsync(long id)
    {
        var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id);
        return game == null ? null : _mapper.ToGameDto(game);
    }

    public async Task<GameDto> CreateGameAsync(GameRequestDto request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var tags = await _db.GameTags.Where(t => request.TagsId.Contains(t.Id)).ToListAsync();
        var game = new Game(request.Name)
        {
            NameEn = request.NameEn,
            Aliases = request.Aliases,
            Company = request.Company,
            Description = request.Description,
            Cover = request.Cover,
            Icon = request.Icon,
            Tags = tags,
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        _embeddingSync.Schedule(EmbeddingTarget.Game, game.Id);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return _mapper.ToGameDto(game);
    }

    public async Task<GameDto> UpdateGameAsync(long id, GameRequestDto request)
    {
        var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException("游戏不存在");

        game.UpdateDetails(request.Name, request.NameEn, request.Aliases, request.Company, request.Description, request.Cover, request.Icon);

        var tags = await _db.GameTags.Where(t => request.TagsId.Contains(t.Id)).ToListAsync();
        game.UpdateTags(tags);

        var recruitments = await _db.Recruitments.Include(r => r.GameTags)
            .Where(r => r.GameId == id).ToListAsync();
        foreach (var rec in recruitments)
        {
            rec.GameTags = [.. tags];
        }

        _embeddingSync.Schedule(EmbeddingTarget.Game, game.Id);
        _embeddingSync.Schedule(EmbeddingTarget.User,
            await GetAffectedUserIdsAsync(recruitments.Select(recruitment => recruitment.Id)));
        await _db.SaveChangesAsync();
        return _mapper.ToGameDto(game);
    }

    public async Task<GameDto> UpdateGameImageAsync(long id, string cover, string icon)
    {
        var game = await _db.Games.FindAsync(id)
            ?? throw new KeyNotFoundException("游戏不存在");

        game.UpdateDetails(game.Name, game.NameEn, game.Aliases, game.Company, game.Description, cover, icon);
        await _db.SaveChangesAsync();
        return _mapper.ToGameDto(game);
    }

    public async Task<bool> DeleteGameAsync(long id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return false;

        var recruitments = await _db.Recruitments
            .Where(r => r.GameId == id)
            .ToListAsync();
        var affectedUsers = await GetAffectedUserIdsAsync(recruitments.Select(recruitment => recruitment.Id));

        foreach (var recruitment in recruitments)
        {
            recruitment.GameId = null;
            recruitment.GameName = game.Name;
        }

        _db.Games.Remove(game);
        _embeddingSync.Schedule(EmbeddingTarget.Game, game.Id);
        _embeddingSync.Schedule(EmbeddingTarget.User, affectedUsers);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<IReadOnlyCollection<long>> GetAffectedUserIdsAsync(IEnumerable<long> recruitmentIds)
    {
        var ids = recruitmentIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var publishers = _db.Recruitments.Where(item => ids.Contains(item.Id))
            .Select(item => item.PublisherId);
        var responders = _db.Responses.Where(item => ids.Contains(item.RecruitmentId))
            .Select(item => item.ResponserId);
        var viewers = _db.RecruitmentViews.Where(item => ids.Contains(item.RecruitmentId))
            .Select(item => item.UserId);
        return await publishers.Concat(responders).Concat(viewers).Distinct().ToListAsync();
    }
}
