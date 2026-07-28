namespace SE26Project_18.Api.Services.Recommendations;

internal static class RecommendationScorer
{
    private const double UserWeight = 0.25;
    private const double RecruitmentWeight = 0.40;
    private const double GameWeight = 0.35;

    public static double? CombineUserCompatibility(
        double? ownToInterestedScore,
        double? interestedToOwnScore
    )
    {
        var score = 0d;
        var count = 0;

        if (ownToInterestedScore.HasValue)
        {
            score += ownToInterestedScore.Value;
            count++;
        }

        if (interestedToOwnScore.HasValue)
        {
            score += interestedToOwnScore.Value;
            count++;
        }

        return count == 0 ? null : score / count;
    }

    public static double Combine(
        double? userCompatibility,
        double? recruitmentTagSimilarity,
        double? gameTagSimilarity
    )
    {
        var weightedScore = 0d;
        var totalWeight = 0d;

        AddSignal(userCompatibility, UserWeight, ref weightedScore, ref totalWeight);
        AddSignal(recruitmentTagSimilarity, RecruitmentWeight, ref weightedScore, ref totalWeight);
        AddSignal(gameTagSimilarity, GameWeight, ref weightedScore, ref totalWeight);

        return totalWeight == 0d ? 0d : weightedScore / totalWeight;
    }

    public static double NormalizeCosine(float score)
    {
        return Math.Clamp((score + 1d) / 2d, 0d, 1d);
    }

    private static void AddSignal(
        double? score,
        double weight,
        ref double weightedScore,
        ref double totalWeight
    )
    {
        if (!score.HasValue)
            return;

        weightedScore += score.Value * weight;
        totalWeight += weight;
    }
}
