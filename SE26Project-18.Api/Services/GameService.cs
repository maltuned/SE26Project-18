using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Services;

internal sealed class GameService : IGameService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingSyncScheduler _embeddingSync;

    public GameService(AppDbContext db, IEmbeddingSyncScheduler embeddingSync)
    {
        _db = db;
        _embeddingSync = embeddingSync;
    }

    public async Task<IReadOnlyCollection<GameResponse>> SearchAsync(
        SearchGamesRequest request,
        CancellationToken ct
    )
    {
        var query = _db.Games.Include(g => g.Tags).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(g =>
                g.Name.Contains(request.Query) || g.Description.Contains(request.Query)
            );
        }

        if (request.TagIds is { Count: > 0 })
        {
            foreach (var tagId in request.TagIds)
            {
                query = query.Where(g => g.Tags.Any(t => t.Id == tagId));
            }
        }

        var games = await query.OrderBy(g => g.Name).Take(10).ToListAsync(ct);

        return games.Select(g => g.ToResponse()).ToList();
    }

    public async Task<GameResponse?> GetById(long id, CancellationToken ct)
    {
        var game = await _db
            .Games.Include(g => g.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (game is null)
        {
            return null;
        }

        return game.ToResponse();
    }

    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken ct)
    {
        ValidateName(request.Name);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        if (await _db.Games.AnyAsync(game => game.Name == request.Name.Trim(), ct))
        {
            throw new ConflictException("Game already exists.");
        }
        var tags = await GetTagsAsync(request.TagIds, ct);
        var game = new Game(request.Name.Trim()) { Description = request.Description, Tags = tags };
        _db.Games.Add(game);
        await _db.SaveChangesAsync(ct);
        _embeddingSync.Schedule(EmbeddingTarget.Game, game.Id);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return game.ToResponse();
    }

    public async Task<GameResponse> UpdateAsync(
        long id,
        UpdateGameRequest request,
        CancellationToken ct
    )
    {
        var game =
            await _db
                .Games.Include(item => item.Tags)
                .FirstOrDefaultAsync(item => item.Id == id, ct)
            ?? throw new NotFoundException("Game not found.");
        if (request.Name is not null)
        {
            ValidateName(request.Name);
            var name = request.Name.Trim();
            if (await _db.Games.AnyAsync(item => item.Id != id && item.Name == name, ct))
            {
                throw new ConflictException("Game already exists.");
            }
            game.Name = name;
        }
        if (request.Description is not null)
        {
            game.Description = request.Description;
        }

        if (request.TagIds is not null)
        {
            game.Tags = await GetTagsAsync(request.TagIds, ct);
            _embeddingSync.Schedule(EmbeddingTarget.Game, id);
            _embeddingSync.Schedule(
                EmbeddingTarget.User,
                await GetUsersAffectedByGameAsync(id, ct)
            );
        }

        await _db.SaveChangesAsync(ct);
        return game.ToResponse();
    }

    private async Task<List<GameTag>> GetTagsAsync(
        IReadOnlyCollection<long>? requestedIds,
        CancellationToken ct
    )
    {
        var ids = requestedIds?.Distinct().ToArray() ?? [];
        var tags = await _db.GameTags.Where(tag => ids.Contains(tag.Id)).ToListAsync(ct);
        if (tags.Count != ids.Length)
        {
            throw new NotFoundException("One or more game tags do not exist.");
        }
        return tags;
    }

    private async Task<IReadOnlyCollection<long>> GetUsersAffectedByGameAsync(
        long gameId,
        CancellationToken ct
    )
    {
        var recruiters = _db
            .Recruitments.Where(item => item.Game.Id == gameId)
            .Select(item => item.Recruiter.Id);
        var responders = _db
            .Responses.Where(item => item.Recruitment.Game.Id == gameId)
            .Select(item => item.Responder.Id);
        var viewers = _db
            .RecruitmentViews.Where(item => item.Recruitment.Game.Id == gameId)
            .Select(item => item.User.Id);
        return await recruiters.Concat(responders).Concat(viewers).Distinct().ToListAsync(ct);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Game name is required.");
        }
    }
}
