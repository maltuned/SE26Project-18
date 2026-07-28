using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Repositories;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class EmbeddingRecruitmentRecommendationAlgorithm
    : IRecruitmentRecommendationAlgorithm
{
    private readonly RecommendationVectorRepository _vectorRepository;
    private readonly IUserPreferenceProfileBuilder _profileBuilder;

    public EmbeddingRecruitmentRecommendationAlgorithm(
        RecommendationVectorRepository vectorRepository,
        IUserPreferenceProfileBuilder profileBuilder
    )
    {
        _vectorRepository = vectorRepository;
        _profileBuilder = profileBuilder;
    }

    public async Task<IReadOnlyList<long>> RankAsync(
        long userId,
        IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
        CancellationToken ct
    )
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var profile = await _profileBuilder.BuildAsync(userId, ct);
        var recruiterIds = candidates
            .Select(candidate => candidate.RecruiterId)
            .Distinct()
            .ToArray();
        var recruitmentIds = candidates.Select(candidate => candidate.Id).ToArray();
        var gameIds = candidates.Select(candidate => candidate.GameId).Distinct().ToArray();

        var ownToInterestedTask = profile.OwnUserTagVector.HasValue
            ? _vectorRepository.SearchUsersByInterestedTagAsync(
                profile.OwnUserTagVector.Value,
                recruiterIds,
                ct
            )
            : EmptySearchAsync();
        var interestedToOwnTask = profile.InterestedUserTagVector.HasValue
            ? _vectorRepository.SearchUsersByOwnTagAsync(
                profile.InterestedUserTagVector.Value,
                recruiterIds,
                ct
            )
            : EmptySearchAsync();
        var recruitmentTask = profile.RecruitmentTagVector.HasValue
            ? _vectorRepository.SearchRecruitmentsByRecruitmentTagAsync(
                profile.RecruitmentTagVector.Value,
                recruitmentIds,
                ct
            )
            : EmptySearchAsync();
        var gameTask = profile.GameTagVector.HasValue
            ? _vectorRepository.SearchGamesByGameTagAsync(profile.GameTagVector.Value, gameIds, ct)
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
            ? GetScoreOrNull(ownToInterestedScores, candidate.RecruiterId)
            : null;
        double? interestedToOwn = profile.InterestedUserTagVector.HasValue
            ? GetScoreOrNull(interestedToOwnScores, candidate.RecruiterId)
            : null;
        var userCompatibility = RecommendationScorer.CombineUserCompatibility(
            ownToInterested,
            interestedToOwn
        );
        double? recruitmentSimilarity = profile.RecruitmentTagVector.HasValue
            ? GetScoreOrNull(recruitmentScores, candidate.Id)
            : null;
        double? gameSimilarity = profile.GameTagVector.HasValue
            ? GetScoreOrNull(gameScores, candidate.GameId)
            : null;

        return RecommendationScorer.Combine(
            userCompatibility,
            recruitmentSimilarity,
            gameSimilarity
        );
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

    private static double? GetScoreOrNull(IReadOnlyDictionary<long, double> scores, long id)
    {
        return scores.TryGetValue(id, out var score) ? score : null;
    }
}
