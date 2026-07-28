using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Recommendations;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class UserPreferenceProfileBuilder : IUserPreferenceProfileBuilder
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly int _dimension;

    public UserPreferenceProfileBuilder(
        AppDbContext db,
        IEmbeddingService embeddingService,
        IOptions<EmbeddingOptions> options
    )
    {
        _db = db;
        _embeddingService = embeddingService;
        _dimension = options.Value.Dimension;
    }

    public async Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct)
    {
        var user =
            await _db
                .Users.Include(u => u.Tags)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        var publishedRecruitments = await _db
            .Recruitments.Where(r => r.Recruiter.Id == userId)
            .Include(r => r.Tags)
            .Include(r => r.Game)
                .ThenInclude(g => g.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);

        var responses = await _db
            .Responses.Where(r => r.Responder.Id == userId)
            .Include(r => r.Recruitment)
                .ThenInclude(r => r.Tags)
            .Include(r => r.Recruitment)
                .ThenInclude(r => r.Game)
                    .ThenInclude(g => g.Tags)
            .Include(r => r.Recruitment)
                .ThenInclude(r => r.Recruiter)
                    .ThenInclude(u => u.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);

        var views = await _db
            .RecruitmentViews.Where(v => v.User.Id == userId)
            .Include(v => v.Recruitment)
                .ThenInclude(r => r.Tags)
            .Include(v => v.Recruitment)
                .ThenInclude(r => r.Game)
                    .ThenInclude(g => g.Tags)
            .Include(v => v.Recruitment)
                .ThenInclude(r => r.Recruiter)
                    .ThenInclude(u => u.Tags)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);

        var ownUserTags = new Dictionary<long, WeightedTag>();
        var interestedUserTags = new Dictionary<long, WeightedTag>();
        var recruitmentTags = new Dictionary<long, WeightedTag>();
        var gameTags = new Dictionary<long, WeightedTag>();

        foreach (var tag in user.Tags)
            AddWeight(ownUserTags, tag.Id, tag.Name, 1d);

        foreach (var recruitment in publishedRecruitments)
        {
            foreach (var tag in recruitment.Tags)
                AddWeight(
                    recruitmentTags,
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Published
                );
            foreach (var tag in recruitment.Game.Tags)
                AddWeight(gameTags, tag.Id, tag.Name, RecommendationBehaviorWeights.Published);
        }

        foreach (var response in responses)
        {
            foreach (var tag in response.Recruitment.Recruiter.Tags)
                AddWeight(
                    interestedUserTags,
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            foreach (var tag in response.Recruitment.Tags)
                AddWeight(
                    recruitmentTags,
                    tag.Id,
                    tag.Name,
                    RecommendationBehaviorWeights.Response
                );
            foreach (var tag in response.Recruitment.Game.Tags)
                AddWeight(gameTags, tag.Id, tag.Name, RecommendationBehaviorWeights.Response);
        }

        foreach (var view in views)
        {
            var weight = RecommendationBehaviorWeights.GetViewWeight(view.ViewCount);
            foreach (var tag in view.Recruitment.Recruiter.Tags)
                AddWeight(interestedUserTags, tag.Id, tag.Name, weight);
            foreach (var tag in view.Recruitment.Tags)
                AddWeight(recruitmentTags, tag.Id, tag.Name, weight);
            foreach (var tag in view.Recruitment.Game.Tags)
                AddWeight(gameTags, tag.Id, tag.Name, weight);
        }

        return new UserPreferenceProfile(
            await BuildVectorAsync(ownUserTags.Values, "user tag", ct),
            await BuildVectorAsync(interestedUserTags.Values, "user tag", ct),
            await BuildVectorAsync(recruitmentTags.Values, "recruitment tag", ct),
            await BuildVectorAsync(gameTags.Values, "game tag", ct)
        );
    }

    internal async Task<ReadOnlyMemory<float>?> BuildVectorAsync(
        IReadOnlyCollection<WeightedTag> tags,
        string category,
        CancellationToken ct
    )
    {
        if (tags.Count == 0)
            return null;

        var texts = tags.Select(tag => $"{category}: {tag.Name}").ToArray();
        var embeddings = await _embeddingService.EmbedAsync(texts, ct);
        return WeightedEmbeddingAggregator.Aggregate(
            tags.Select(tag => (embeddings[$"{category}: {tag.Name}"], tag.Weight)).ToList(),
            _dimension
        );
    }

    private static void AddWeight(
        IDictionary<long, WeightedTag> tags,
        long id,
        string name,
        double weight
    )
    {
        tags[id] = tags.TryGetValue(id, out var existing)
            ? existing with
            {
                Weight = existing.Weight + weight,
            }
            : new WeightedTag(id, name, weight);
    }

    internal sealed record WeightedTag(long Id, string Name, double Weight);
}
