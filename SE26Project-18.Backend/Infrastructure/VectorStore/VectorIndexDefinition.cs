namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal sealed record VectorIndexDefinition(
    string Name,
    IReadOnlyCollection<VectorFieldDefinition> Fields,
    VectorDistanceMetric Metric
);
