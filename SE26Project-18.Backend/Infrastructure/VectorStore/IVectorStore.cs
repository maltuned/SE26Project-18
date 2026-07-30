namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal interface IVectorStore
{
    Task EnsureIndexAsync(VectorIndexDefinition definition, CancellationToken ct);

    Task UpsertAsync(VectorRecord record, CancellationToken ct);

    Task UpsertManyAsync(IReadOnlyCollection<VectorRecord> records, CancellationToken ct);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken ct
    );

    Task DeleteAsync(string indexName, IReadOnlyCollection<long> ids, CancellationToken ct);
}
