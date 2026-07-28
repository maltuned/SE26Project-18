using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Tests.Infrastructure.Embedding;

public sealed class WeightedEmbeddingAggregatorTests
{
    [Fact]
    public void Aggregate_AppliesWeightsAndNormalizesResult()
    {
        var result = WeightedEmbeddingAggregator.Aggregate(
            new List<(ReadOnlyMemory<float>, double)>
            {
                (new float[] { 1f, 0f }, 3d),
                (new float[] { 0f, 1f }, 1d),
            },
            2
        );

        Assert.Equal(3d / Math.Sqrt(10d), result.Span[0], precision: 6);
        Assert.Equal(1d / Math.Sqrt(10d), result.Span[1], precision: 6);
    }
}
