namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal sealed record VectorRecord(
    string IndexName,
    long Id,
    IReadOnlyDictionary<string, ReadOnlyMemory<float>> Vectors
);
