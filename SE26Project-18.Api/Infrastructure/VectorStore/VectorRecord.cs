namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed record VectorRecord(
    string IndexName,
    long Id,
    IReadOnlyDictionary<string, ReadOnlyMemory<float>> Vectors
);
