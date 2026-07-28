using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.VectorProfiles;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class EmbeddingProfileBatchBuilder
{
    private readonly AppDbContext _db;
    private readonly TagEmbeddingBuilder _tagEmbeddingBuilder;

    public EmbeddingProfileBatchBuilder(AppDbContext db, TagEmbeddingBuilder tagEmbeddingBuilder)
    {
        _db = db;
        _tagEmbeddingBuilder = tagEmbeddingBuilder;
    }

    public async Task<IReadOnlyCollection<UserVectorProfile>> BuildUsersAsync(
        IReadOnlyCollection<long> userIds,
        CancellationToken ct
    )
    {
        var ids = userIds.Distinct().ToArray();
        var ownUserTags = CreateAccumulators(ids);
        var interestedUserTags = CreateAccumulators(ids);
        var recruitmentTags = CreateAccumulators(ids);
        var gameTags = CreateAccumulators(ids);
        var users = await _db
            .Users.Where(user => ids.Contains(user.Id))
            .Include(user => user.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        var existingIds = users.Select(user => user.Id).ToHashSet();

        foreach (var user in users)
        {
            foreach (var tag in user.Tags)
            {
                AddWeight(ownUserTags[user.Id], tag.Id, tag.Name, 1d);
            }
        }

        var published = await _db
            .Recruitments.Where(recruitment => ids.Contains(recruitment.Recruiter.Id))
            .Include(recruitment => recruitment.Recruiter)
            .Include(recruitment => recruitment.Tags)
            .Include(recruitment => recruitment.Game)
                .ThenInclude(game => game.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        foreach (var recruitment in published)
        {
            var userId = recruitment.Recruiter.Id;
            foreach (var tag in recruitment.Tags)
            {
                AddWeight(
                    recruitmentTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Published
                );
            }
            foreach (var tag in recruitment.Game.Tags)
            {
                AddWeight(
                    gameTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Published
                );
            }
        }

        var responses = await _db
            .Responses.Where(response => ids.Contains(response.Responder.Id))
            .Include(response => response.Responder)
            .Include(response => response.Recruitment)
                .ThenInclude(recruitment => recruitment.Tags)
            .Include(response => response.Recruitment)
                .ThenInclude(recruitment => recruitment.Game)
                    .ThenInclude(game => game.Tags)
            .Include(response => response.Recruitment)
                .ThenInclude(recruitment => recruitment.Recruiter)
                    .ThenInclude(user => user.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        foreach (var response in responses)
        {
            var userId = response.Responder.Id;
            foreach (var tag in response.Recruitment.Recruiter.Tags)
            {
                AddWeight(
                    interestedUserTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            }
            foreach (var tag in response.Recruitment.Tags)
            {
                AddWeight(
                    recruitmentTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            }
            foreach (var tag in response.Recruitment.Game.Tags)
            {
                AddWeight(
                    gameTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            }
        }

        var views = await _db
            .RecruitmentViews.Where(view => ids.Contains(view.User.Id))
            .Include(view => view.User)
            .Include(view => view.Recruitment)
                .ThenInclude(recruitment => recruitment.Tags)
            .Include(view => view.Recruitment)
                .ThenInclude(recruitment => recruitment.Game)
                    .ThenInclude(game => game.Tags)
            .Include(view => view.Recruitment)
                .ThenInclude(recruitment => recruitment.Recruiter)
                    .ThenInclude(user => user.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        foreach (var view in views)
        {
            var userId = view.User.Id;
            var weight = RecommendationBehaviorWeights.GetViewWeight(view.ViewCount);
            foreach (var tag in view.Recruitment.Recruiter.Tags)
            {
                AddWeight(interestedUserTags[userId], tag.Id, tag.Name, weight);
            }
            foreach (var tag in view.Recruitment.Tags)
            {
                AddWeight(recruitmentTags[userId], tag.Id, tag.Name, weight);
            }
            foreach (var tag in view.Recruitment.Game.Tags)
            {
                AddWeight(gameTags[userId], tag.Id, tag.Name, weight);
            }
        }

        var accepted = await _db
            .Responses.Where(response =>
                ids.Contains(response.Recruitment.Recruiter.Id)
                && response.Type == ResponseType.Accepted
            )
            .Include(response => response.Recruitment)
                .ThenInclude(recruitment => recruitment.Recruiter)
            .Include(response => response.Responder)
                .ThenInclude(user => user.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        foreach (var response in accepted)
        {
            var userId = response.Recruitment.Recruiter.Id;
            foreach (var tag in response.Responder.Tags)
            {
                AddWeight(
                    interestedUserTags[userId],
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            }
        }

        var ownVectors = await BuildVectorsAsync(ownUserTags, "user tag", ct);
        var interestedVectors = await BuildVectorsAsync(interestedUserTags, "user tag", ct);
        var recruitmentVectors = await BuildVectorsAsync(recruitmentTags, "recruitment tag", ct);
        var gameVectors = await BuildVectorsAsync(gameTags, "game tag", ct);

        return ids.Select(id => new UserVectorProfile(
                id,
                existingIds.Contains(id) ? ownVectors[id] : null,
                existingIds.Contains(id) ? interestedVectors[id] : null,
                existingIds.Contains(id) ? recruitmentVectors[id] : null,
                existingIds.Contains(id) ? gameVectors[id] : null
            ))
            .ToList();
    }

    public async Task<IReadOnlyCollection<GameVectorProfile>> BuildGamesAsync(
        IReadOnlyCollection<long> gameIds,
        CancellationToken ct
    )
    {
        var ids = gameIds.Distinct().ToArray();
        var games = await _db
            .Games.Where(game => ids.Contains(game.Id))
            .Include(game => game.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        var inputs = games.ToDictionary(
            game => game.Id,
            game =>
                (IReadOnlyCollection<WeightedTagInput>)
                    game.Tags.Select(tag => new WeightedTagInput(tag.Id, tag.Name, 1d)).ToList()
        );
        var vectors = await _tagEmbeddingBuilder.BuildManyAsync(inputs, "game tag", ct);

        return ids.Select(id => new GameVectorProfile(
                id,
                vectors.TryGetValue(id, out var vector) ? vector : null
            ))
            .ToList();
    }

    public async Task<IReadOnlyCollection<RecruitmentVectorProfile>> BuildRecruitmentsAsync(
        IReadOnlyCollection<long> recruitmentIds,
        CancellationToken ct
    )
    {
        var ids = recruitmentIds.Distinct().ToArray();
        var recruitments = await _db
            .Recruitments.Where(recruitment => ids.Contains(recruitment.Id))
            .Include(recruitment => recruitment.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);
        var inputs = recruitments
            .Where(recruitment => recruitment.Status != RecruitmentStatus.Deleted)
            .ToDictionary(
                recruitment => recruitment.Id,
                recruitment =>
                    (IReadOnlyCollection<WeightedTagInput>)
                        recruitment
                            .Tags.Select(tag => new WeightedTagInput(tag.Id, tag.Name, 1d))
                            .ToList()
            );
        var vectors = await _tagEmbeddingBuilder.BuildManyAsync(inputs, "recruitment tag", ct);

        return ids.Select(id => new RecruitmentVectorProfile(
                id,
                vectors.TryGetValue(id, out var vector) ? vector : null
            ))
            .ToList();
    }

    private Task<IReadOnlyDictionary<long, ReadOnlyMemory<float>?>> BuildVectorsAsync(
        IReadOnlyDictionary<long, Dictionary<long, WeightedTagInput>> accumulators,
        string category,
        CancellationToken ct
    )
    {
        return _tagEmbeddingBuilder.BuildManyAsync(
            accumulators.ToDictionary(
                item => item.Key,
                item => (IReadOnlyCollection<WeightedTagInput>)item.Value.Values.ToList()
            ),
            category,
            ct
        );
    }

    private static Dictionary<long, Dictionary<long, WeightedTagInput>> CreateAccumulators(
        IEnumerable<long> ids
    )
    {
        return ids.ToDictionary(id => id, _ => new Dictionary<long, WeightedTagInput>());
    }

    private static void AddWeight(
        IDictionary<long, WeightedTagInput> tags,
        long tagId,
        string name,
        double weight
    )
    {
        tags[tagId] = tags.TryGetValue(tagId, out var existing)
            ? existing with
            {
                Weight = existing.Weight + weight,
            }
            : new WeightedTagInput(tagId, name, weight);
    }
}
