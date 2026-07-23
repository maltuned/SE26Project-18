using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public sealed class GameService : IGameService
{
    private readonly AppDbContext _db;

    public GameService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<GameResponse>> SearchAsync(
        SearchGamesRequest request,
        CancellationToken ct
    )
    {
        var query = _db.Games.Include(g => g.Tags).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
            query = query.Where(g =>
                g.Name.Contains(request.Query) || g.Description.Contains(request.Query)
            );

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
            return null;

        return game.ToResponse();
    }
}
