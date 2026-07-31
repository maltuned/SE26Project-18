namespace SE26Project_18.Backend.Services.Recommendations;

internal static class RecommendationScorer
{
    private const double RecruitmentWeight = 0.65;
    private const double GameWeight = 0.35;

    public static double Combine(double? recruitmentSimilarity, double? gameSimilarity)
    {
        var score = 0d;
        var weight = 0d;
        Add(recruitmentSimilarity, RecruitmentWeight, ref score, ref weight);
        Add(gameSimilarity, GameWeight, ref score, ref weight);
        return weight == 0d ? 0d : score / weight;
    }

    public static double NormalizeCosine(float score) => Math.Clamp((score + 1d) / 2d, 0d, 1d);

    private static void Add(double? value, double signalWeight, ref double score, ref double weight)
    {
        if (!value.HasValue) return;
        score += value.Value * signalWeight;
        weight += signalWeight;
    }
}
