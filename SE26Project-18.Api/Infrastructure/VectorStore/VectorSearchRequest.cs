namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed record VectorSearchRequest(
    string IndexName,
    string VectorFieldName,
    ReadOnlyMemory<float> QueryVector,
    int Limit
);
