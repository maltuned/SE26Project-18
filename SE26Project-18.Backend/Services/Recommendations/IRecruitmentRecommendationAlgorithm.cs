using SE26Project_18.Backend.Models.Recommendations;

namespace SE26Project_18.Backend.Services.Recommendations;

public interface IRecruitmentRecommendationAlgorithm
{
    Task<IReadOnlyList<long>> RankAsync(
        long userId,
        IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
        CancellationToken ct
    );
}
