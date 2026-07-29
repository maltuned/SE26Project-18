using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.Messaging;
using SE26Project_18.Api.Repositories;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class EmbeddingSyncBatchConsumer : IBatchEventConsumer<EmbeddingSyncRequested>
{
    private readonly EmbeddingProfileBatchBuilder _profileBuilder;

    private readonly RecommendationVectorRepository _repository;

    private readonly AppDbContext _db;

    public EmbeddingSyncBatchConsumer(
        EmbeddingProfileBatchBuilder profileBuilder,
        RecommendationVectorRepository repository,
        AppDbContext db
    )
    {
        _profileBuilder = profileBuilder;
        _repository = repository;
        _db = db;
    }

    public async Task ConsumeAsync(
        IReadOnlyCollection<EmbeddingSyncRequested> events,
        CancellationToken ct
    )
    {
        foreach (var message in events)
        {
            if (!Enum.IsDefined(message.Target) || message.EntityId <= 0 || message.Version <= 0)
            {
                throw new EmbeddingSyncValidationException(
                    "Embedding sync event contains an invalid target or entity ID."
                );
            }
        }

        var unique = events
            .GroupBy(message => (message.Target, message.EntityId))
            .Select(group => group.MaxBy(message => message.Version)!)
            .ToList();
        await SynchronizeUsersAsync(GetEvents(unique, EmbeddingTarget.User), ct);
        await SynchronizeGamesAsync(GetEvents(unique, EmbeddingTarget.Game), ct);
        await SynchronizeRecruitmentsAsync(GetEvents(unique, EmbeddingTarget.Recruitment), ct);
    }

    private async Task SynchronizeUsersAsync(
        IReadOnlyCollection<EmbeddingSyncRequested> events,
        CancellationToken ct
    )
    {
        if (events.Count == 0)
        {
            return;
        }

        var versions = events.ToDictionary(message => message.EntityId, message => message.Version);
        var ids = versions.Keys.ToArray();
        var users = await _db.Users.Where(user => ids.Contains(user.Id)).ToListAsync(ct);
        var profiles = await _profileBuilder.BuildUsersAsync(ids, ct);
        await _repository.SynchronizeUserProfilesAsync(profiles, ct);
        foreach (var user in users)
        {
            user.MarkEmbeddingApplied(versions[user.Id]);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SynchronizeGamesAsync(
        IReadOnlyCollection<EmbeddingSyncRequested> events,
        CancellationToken ct
    )
    {
        if (events.Count == 0)
        {
            return;
        }

        var versions = events.ToDictionary(message => message.EntityId, message => message.Version);
        var ids = versions.Keys.ToArray();
        var games = await _db.Games.Where(game => ids.Contains(game.Id)).ToListAsync(ct);
        var profiles = await _profileBuilder.BuildGamesAsync(ids, ct);
        await _repository.SynchronizeGameProfilesAsync(profiles, ct);
        foreach (var game in games)
        {
            game.MarkEmbeddingApplied(versions[game.Id]);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SynchronizeRecruitmentsAsync(
        IReadOnlyCollection<EmbeddingSyncRequested> events,
        CancellationToken ct
    )
    {
        if (events.Count == 0)
        {
            return;
        }

        var versions = events.ToDictionary(message => message.EntityId, message => message.Version);
        var ids = versions.Keys.ToArray();
        var recruitments = await _db
            .Recruitments.Where(recruitment => ids.Contains(recruitment.Id))
            .ToListAsync(ct);
        var profiles = await _profileBuilder.BuildRecruitmentsAsync(ids, ct);
        await _repository.SynchronizeRecruitmentProfilesAsync(profiles, ct);
        foreach (var recruitment in recruitments)
        {
            recruitment.MarkEmbeddingApplied(versions[recruitment.Id]);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static IReadOnlyCollection<EmbeddingSyncRequested> GetEvents(
        IEnumerable<EmbeddingSyncRequested> events,
        EmbeddingTarget target
    )
    {
        return events.Where(message => message.Target == target).ToList();
    }
}

internal sealed class EmbeddingSyncValidationException(string message) : Exception(message);
