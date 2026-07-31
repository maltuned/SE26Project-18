namespace SE26Project_18.Backend.Services.Recommendations;

internal static class RecommendationBehaviorWeights
{
    public const double Published = 3d;

    public const double Response = 3d;

    private const double View = 0.5d;

    private const double MaximumView = 1.5d;

    public static double GetViewWeight(int viewCount)
    {
        return Math.Min(Math.Max(viewCount, 0) * View, MaximumView);
    }
}
