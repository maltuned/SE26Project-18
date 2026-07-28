using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Repositories;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class EmbeddingRecruitmentRecommendationAlgorithm
    : IRecruitmentRecommendationAlgorithm
{
    private const int MaximumSearchLimit = 16_384;

    private readonly RecommendationVectorRepository _vectorRepository;
    private readonly IUserPreferenceProfileBuilder _profileBuilder;
    private readonly ILogger<EmbeddingRecruitmentRecommendationAlgorithm> _logger;

    public EmbeddingRecruitmentRecommendationAlgorithm(
        RecommendationVectorRepository vectorRepository,
        IUserPreferenceProfileBuilder profileBuilder,
        ILogger<EmbeddingRecruitmentRecommendationAlgorithm> logger
    )
    {
        _vectorRepository = vectorRepository;
        _profileBuilder = profileBuilder;
        _logger = logger;
    }

    public async Task<IReadOnlyList<long>> RankAsync(
        long userId,
        IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
        CancellationToken ct
    )
    {
        if (candidates.Count == 0)
            return [];

        var profile = await TryBuildProfileAsync(userId, ct);
        var searchLimit = Math.Min(Math.Max(candidates.Count * 5, 100), MaximumSearchLimit);

        var ownToInterestedTask = profile.OwnUserTagVector.HasValue
            ? _vectorRepository.SearchUsersByInterestedTagAsync(
                profile.OwnUserTagVector.Value,
                searchLimit,
                ct
            )
            : EmptySearchAsync();
        var interestedToOwnTask = profile.InterestedUserTagVector.HasValue
            ? _vectorRepository.SearchUsersByOwnTagAsync(
                profile.InterestedUserTagVector.Value,
                searchLimit,
                ct
            )
            : EmptySearchAsync();
        var recruitmentTask = profile.RecruitmentTagVector.HasValue
            ? _vectorRepository.SearchRecruitmentsByRecruitmentTagAsync(
                profile.RecruitmentTagVector.Value,
                searchLimit,
                ct
            )
            : EmptySearchAsync();
        var gameTask = profile.GameTagVector.HasValue
            ? _vectorRepository.SearchGamesByGameTagAsync(
                profile.GameTagVector.Value,
                searchLimit,
                ct
            )
            : EmptySearchAsync();

        await Task.WhenAll(ownToInterestedTask, interestedToOwnTask, recruitmentTask, gameTask);

        var ownToInterestedScores = ToScoreMap(await ownToInterestedTask);
        var interestedToOwnScores = ToScoreMap(await interestedToOwnTask);
        var recruitmentScores = ToScoreMap(await recruitmentTask);
        var gameScores = ToScoreMap(await gameTask);

        return candidates
            .Select(candidate => new
            {
                candidate.Id,
                Score = CalculateScore(
                    candidate,
                    profile,
                    ownToInterestedScores,
                    interestedToOwnScores,
                    recruitmentScores,
                    gameScores
                ),
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Id)
            .Select(item => item.Id)
            .ToList();
    }

    private static double CalculateScore(
        RecruitmentRecommendationCandidate candidate,
        UserPreferenceProfile profile,
        IReadOnlyDictionary<long, double> ownToInterestedScores,
        IReadOnlyDictionary<long, double> interestedToOwnScores,
        IReadOnlyDictionary<long, double> recruitmentScores,
        IReadOnlyDictionary<long, double> gameScores
    )
    {
        double? ownToInterested = profile.OwnUserTagVector.HasValue
            ? GetScore(ownToInterestedScores, candidate.RecruiterId)
            : null;
        double? interestedToOwn = profile.InterestedUserTagVector.HasValue
            ? GetScore(interestedToOwnScores, candidate.RecruiterId)
            : null;
        var userCompatibility = RecommendationScorer.CombineUserCompatibility(
            ownToInterested,
            interestedToOwn
        );
        double? recruitmentSimilarity = profile.RecruitmentTagVector.HasValue
            ? GetScore(recruitmentScores, candidate.Id)
            : null;
        double? gameSimilarity = profile.GameTagVector.HasValue
            ? GetScore(gameScores, candidate.GameId)
            : null;

        return RecommendationScorer.Combine(
            userCompatibility,
            recruitmentSimilarity,
            gameSimilarity
        );
    }

    private async Task<UserPreferenceProfile> TryBuildProfileAsync(
        long userId,
        CancellationToken ct
    )
    {
        try
        {
            return await _profileBuilder.BuildAsync(userId, ct);
        }
        catch (Exception exception)
            when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Recommendation embeddings are unavailable for user {UserId}; using newest-first fallback",
                userId
            );
            return new UserPreferenceProfile(null, null, null, null);
        }
    }

    private static Task<IReadOnlyList<VectorSearchResult>> EmptySearchAsync()
    {
        return Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);
    }

    private static Dictionary<long, double> ToScoreMap(IReadOnlyList<VectorSearchResult> results)
    {
        return results.ToDictionary(
            result => result.Id,
            result => RecommendationScorer.NormalizeCosine(result.Score)
        );
    }

    private static double GetScore(IReadOnlyDictionary<long, double> scores, long id)
    {
        return scores.GetValueOrDefault(id);
    }
}
