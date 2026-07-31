namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal sealed record VectorSearchRequest(
    string IndexName,
    string VectorFieldName,
    ReadOnlyMemory<float> QueryVector,
    int Limit,
    IReadOnlyCollection<long>? AllowedIds = null
);
