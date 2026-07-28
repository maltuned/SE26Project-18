using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services.Recommendations;

public sealed class RecommendationScorerTests
{
    [Fact]
    public void Combine_RenormalizesMissingSignals()
    {
        var score = RecommendationScorer.Combine(null, 0.8, 0.4);

        Assert.Equal((0.8 * 0.4 + 0.4 * 0.35) / 0.75, score, precision: 10);
    }

    [Fact]
    public void CombineUserCompatibility_AveragesAvailableDirections()
    {
        var score = RecommendationScorer.CombineUserCompatibility(0.8, 0.4);

        Assert.Equal(0.6, score!.Value, precision: 10);
    }

    [Fact]
    public void NormalizeCosine_MapsRangeToZeroAndOne()
    {
        Assert.Equal(0d, RecommendationScorer.NormalizeCosine(-1f));
        Assert.Equal(0.5d, RecommendationScorer.NormalizeCosine(0f));
        Assert.Equal(1d, RecommendationScorer.NormalizeCosine(1f));
    }
}
