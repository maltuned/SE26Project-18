using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services.Recommendations;

public sealed class RecommendationBehaviorWeightsTests
{
    [Theory]
    [InlineData(0, 0d)]
    [InlineData(1, 0.5d)]
    [InlineData(2, 1d)]
    [InlineData(3, 1.5d)]
    [InlineData(20, 1.5d)]
    public void GetViewWeight_CapsRepeatedViews(int count, double expected)
    {
        Assert.Equal(expected, RecommendationBehaviorWeights.GetViewWeight(count));
    }
}
