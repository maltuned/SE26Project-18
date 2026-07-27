namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed record VectorIndexDefinition(
    string Name,
    IReadOnlyCollection<VectorFieldDefinition> Fields,
    VectorDistanceMetric Metric
);
