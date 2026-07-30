using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Models.VectorProfiles;

namespace SE26Project_18.Backend.Services.Recommendations;

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
        CancellationToken ct)
    {
        var ids = userIds.Distinct().ToArray();
        var existingIds = await _db.Users.Where(user => ids.Contains(user.Id))
            .Select(user => user.Id).ToHashSetAsync(ct);
        var recruitmentTags = CreateAccumulators(ids);
        var gameTags = CreateAccumulators(ids);

        var published = await _db.Recruitments
            .Where(recruitment => ids.Contains(recruitment.PublisherId))
            .Include(recruitment => recruitment.RecruitmentTags)
            .Include(recruitment => recruitment.GameTags)
            .AsNoTracking().AsSplitQuery().ToListAsync(ct);
        foreach (var recruitment in published)
        {
            AddTags(recruitmentTags[recruitment.PublisherId], recruitment.RecruitmentTags,
                RecommendationBehaviorWeights.Published);
            AddTags(gameTags[recruitment.PublisherId], recruitment.GameTags,
                RecommendationBehaviorWeights.Published);
        }

        var responses = await _db.Responses
            .Where(response => ids.Contains(response.ResponserId))
            .Include(response => response.Recruitment).ThenInclude(recruitment => recruitment.RecruitmentTags)
            .Include(response => response.Recruitment).ThenInclude(recruitment => recruitment.GameTags)
            .AsNoTracking().AsSplitQuery().ToListAsync(ct);
        foreach (var response in responses)
        {
            AddTags(recruitmentTags[response.ResponserId], response.Recruitment.RecruitmentTags,
                RecommendationBehaviorWeights.Response);
            AddTags(gameTags[response.ResponserId], response.Recruitment.GameTags,
                RecommendationBehaviorWeights.Response);
        }

        var views = await _db.RecruitmentViews
            .Where(view => ids.Contains(view.UserId))
            .Include(view => view.Recruitment).ThenInclude(recruitment => recruitment.RecruitmentTags)
            .Include(view => view.Recruitment).ThenInclude(recruitment => recruitment.GameTags)
            .AsNoTracking().AsSplitQuery().ToListAsync(ct);
        foreach (var view in views)
        {
            var weight = RecommendationBehaviorWeights.GetViewWeight(view.ViewCount);
            AddTags(recruitmentTags[view.UserId], view.Recruitment.RecruitmentTags, weight);
            AddTags(gameTags[view.UserId], view.Recruitment.GameTags, weight);
        }

        var recruitmentVectors = await BuildVectorsAsync(recruitmentTags, "recruitment tag", ct);
        var gameVectors = await BuildVectorsAsync(gameTags, "game tag", ct);
        return ids.Select(id => new UserVectorProfile(
            id,
            existingIds.Contains(id) ? recruitmentVectors[id] : null,
            existingIds.Contains(id) ? gameVectors[id] : null)).ToList();
    }

    public async Task<IReadOnlyCollection<GameVectorProfile>> BuildGamesAsync(
        IReadOnlyCollection<long> gameIds,
        CancellationToken ct)
    {
        var ids = gameIds.Distinct().ToArray();
        var games = await _db.Games.Where(game => ids.Contains(game.Id))
            .Include(game => game.Tags).AsNoTracking().AsSplitQuery().ToListAsync(ct);
        var inputs = games.ToDictionary(
            game => game.Id,
            game => (IReadOnlyCollection<WeightedTagInput>)game.Tags
                .Select(tag => new WeightedTagInput(tag.Id, tag.Name, 1d)).ToList());
        var vectors = await _tagEmbeddingBuilder.BuildManyAsync(inputs, "game tag", ct);
        return ids.Select(id => new GameVectorProfile(
            id, vectors.TryGetValue(id, out var vector) ? vector : null)).ToList();
    }

    public async Task<IReadOnlyCollection<RecruitmentVectorProfile>> BuildRecruitmentsAsync(
        IReadOnlyCollection<long> recruitmentIds,
        CancellationToken ct)
    {
        var ids = recruitmentIds.Distinct().ToArray();
        var recruitments = await _db.Recruitments.Where(recruitment => ids.Contains(recruitment.Id))
            .Include(recruitment => recruitment.RecruitmentTags)
            .AsNoTracking().AsSplitQuery().ToListAsync(ct);
        var inputs = recruitments.Where(recruitment => recruitment.Status != RecruitmentStatus.Deleted)
            .ToDictionary(
                recruitment => recruitment.Id,
                recruitment => (IReadOnlyCollection<WeightedTagInput>)recruitment.RecruitmentTags
                    .Select(tag => new WeightedTagInput(tag.Id, tag.Name, 1d)).ToList());
        var vectors = await _tagEmbeddingBuilder.BuildManyAsync(inputs, "recruitment tag", ct);
        return ids.Select(id => new RecruitmentVectorProfile(
            id, vectors.TryGetValue(id, out var vector) ? vector : null)).ToList();
    }

    private Task<IReadOnlyDictionary<long, ReadOnlyMemory<float>?>> BuildVectorsAsync(
        IReadOnlyDictionary<long, Dictionary<long, WeightedTagInput>> accumulators,
        string category,
        CancellationToken ct)
    {
        return _tagEmbeddingBuilder.BuildManyAsync(
            accumulators.ToDictionary(
                item => item.Key,
                item => (IReadOnlyCollection<WeightedTagInput>)item.Value.Values.ToList()),
            category,
            ct);
    }

    private static Dictionary<long, Dictionary<long, WeightedTagInput>> CreateAccumulators(
        IEnumerable<long> ids) => ids.ToDictionary(id => id, _ => new Dictionary<long, WeightedTagInput>());

    private static void AddTags<T>(
        IDictionary<long, WeightedTagInput> accumulator,
        IEnumerable<T> tags,
        double weight) where T : class
    {
        foreach (var tag in tags)
        {
            var id = tag switch
            {
                Models.Entities.GameTag gameTag => gameTag.Id,
                Models.Entities.RecruitmentTag recruitmentTag => recruitmentTag.Id,
                _ => throw new ArgumentOutOfRangeException(nameof(tags)),
            };
            var name = tag switch
            {
                Models.Entities.GameTag gameTag => gameTag.Name,
                Models.Entities.RecruitmentTag recruitmentTag => recruitmentTag.Name,
                _ => throw new ArgumentOutOfRangeException(nameof(tags)),
            };
            accumulator[id] = accumulator.TryGetValue(id, out var existing)
                ? existing with { Weight = existing.Weight + weight }
                : new WeightedTagInput(id, name, weight);
        }
    }
}
