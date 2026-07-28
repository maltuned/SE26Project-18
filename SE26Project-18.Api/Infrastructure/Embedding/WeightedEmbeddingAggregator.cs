namespace SE26Project_18.Api.Infrastructure.Embedding;

internal static class WeightedEmbeddingAggregator
{
    public static ReadOnlyMemory<float> Aggregate(
        IReadOnlyCollection<(ReadOnlyMemory<float> Vector, double Weight)> embeddings,
        int dimension
    )
    {
        if (embeddings.Count == 0)
            throw new ArgumentException("At least one embedding is required.", nameof(embeddings));

        var sum = new double[dimension];
        var totalWeight = 0d;
        foreach (var (vector, weight) in embeddings)
        {
            if (vector.Length != dimension)
                throw new ArgumentException(
                    "Embedding dimensions do not match.",
                    nameof(embeddings)
                );
            if (weight <= 0d)
                throw new ArgumentOutOfRangeException(
                    nameof(embeddings),
                    "Weights must be positive."
                );

            for (var i = 0; i < dimension; i++)
                sum[i] += vector.Span[i] * weight;
            totalWeight += weight;
        }

        var result = new float[dimension];
        var squaredNorm = 0d;
        for (var i = 0; i < dimension; i++)
        {
            result[i] = (float)(sum[i] / totalWeight);
            squaredNorm += result[i] * result[i];
        }

        var norm = Math.Sqrt(squaredNorm);
        if (norm == 0d)
            throw new InvalidOperationException("Weighted embeddings produced a zero vector.");

        for (var i = 0; i < result.Length; i++)
            result[i] = (float)(result[i] / norm);

        return result;
    }
}
