using SE26Project_18.Api.Models.Recommendations;

namespace SE26Project_18.Api.Services.Recommendations;

internal interface IRecruitmentRecommendationAlgorithm
{
    Task<IReadOnlyList<long>> RankAsync(
        long userId,
        IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
        CancellationToken ct
    );
}
