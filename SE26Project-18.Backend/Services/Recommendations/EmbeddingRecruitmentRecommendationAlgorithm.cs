using SE26Project_18.Backend.Infrastructure.VectorStore;
using SE26Project_18.Backend.Models.Recommendations;
using SE26Project_18.Backend.Repositories;

namespace SE26Project_18.Backend.Services.Recommendations;

internal sealed class EmbeddingRecruitmentRecommendationAlgorithm : IRecruitmentRecommendationAlgorithm
{
    private readonly RecommendationVectorRepository _repository;
    private readonly IUserPreferenceProfileBuilder _profileBuilder;

    public EmbeddingRecruitmentRecommendationAlgorithm(
        RecommendationVectorRepository repository,
        IUserPreferenceProfileBuilder profileBuilder)
    {
        _repository = repository;
        _profileBuilder = profileBuilder;
    }

    public async Task<IReadOnlyList<long>> RankAsync(
        long userId,
        IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
        CancellationToken ct)
    {
        if (candidates.Count == 0) return [];
        var profile = await _profileBuilder.BuildAsync(userId, ct);
        if (!profile.RecruitmentTagVector.HasValue && !profile.GameTagVector.HasValue)
            return candidates.Select(candidate => candidate.Id).ToList();
        var recruitmentTask = profile.RecruitmentTagVector.HasValue
            ? _repository.SearchRecruitmentsByRecruitmentTagAsync(
                profile.RecruitmentTagVector.Value, candidates.Select(item => item.Id).ToArray(), ct)
            : EmptyAsync();
        var gameIds = candidates.Where(item => item.GameId.HasValue)
            .Select(item => item.GameId!.Value).Distinct().ToArray();
        var gameTask = profile.GameTagVector.HasValue && gameIds.Length > 0
            ? _repository.SearchGamesByGameTagAsync(profile.GameTagVector.Value, gameIds, ct)
            : EmptyAsync();
        await Task.WhenAll(recruitmentTask, gameTask);
        var recruitmentScores = ToScoreMap(await recruitmentTask);
        var gameScores = ToScoreMap(await gameTask);

        return candidates.Select((candidate, index) => new
            {
                candidate.Id,
                OriginalOrder = index,
                Score = RecommendationScorer.Combine(
                    GetScore(recruitmentScores, candidate.Id, profile.RecruitmentTagVector.HasValue),
                    candidate.GameId.HasValue
                        ? GetScore(gameScores, candidate.GameId.Value, profile.GameTagVector.HasValue)
                        : null),
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.OriginalOrder)
            .Select(item => item.Id).ToList();
    }

    private static Task<IReadOnlyList<VectorSearchResult>> EmptyAsync() =>
        Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

    private static Dictionary<long, double> ToScoreMap(IEnumerable<VectorSearchResult> results) =>
        results.ToDictionary(result => result.Id, result => RecommendationScorer.NormalizeCosine(result.Score));

    private static double? GetScore(IReadOnlyDictionary<long, double> scores, long id, bool enabled) =>
        enabled && scores.TryGetValue(id, out var score) ? score : null;
}
