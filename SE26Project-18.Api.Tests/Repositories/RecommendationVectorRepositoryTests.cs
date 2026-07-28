using Microsoft.Extensions.Options;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.VectorProfiles;
using SE26Project_18.Api.Repositories;

namespace SE26Project_18.Api.Tests.Repositories;

public sealed class RecommendationVectorRepositoryTests
{
    [Fact]
    public async Task SynchronizeGameProfiles_BatchesUpsertsAndDeletesEmptyVectors()
    {
        var vectorStore = new RecordingVectorStore();
        var repository = new RecommendationVectorRepository(
            vectorStore,
            Options.Create(new EmbeddingOptions { Dimension = 2 }),
            Options.Create(new EmbeddingSyncOptions { MilvusBatchSize = 100 })
        );

        await repository.SynchronizeGameProfilesAsync(
            [
                new GameVectorProfile(1, new float[] { 1, 0 }),
                new GameVectorProfile(2, null),
                new GameVectorProfile(3, new float[] { 0, 1 }),
            ],
            CancellationToken.None
        );

        var upsert = Assert.Single(vectorStore.UpsertBatches);
        Assert.Equal([1L, 3L], upsert.Select(record => record.Id));
        var deletion = Assert.Single(vectorStore.Deletions);
        Assert.Equal("game_profiles", deletion.IndexName);
        Assert.Equal([2L], deletion.Ids);
    }

    private sealed class RecordingVectorStore : IVectorStore
    {
        public List<IReadOnlyCollection<VectorRecord>> UpsertBatches { get; } = [];
        public List<(string IndexName, IReadOnlyCollection<long> Ids)> Deletions { get; } = [];

        public Task EnsureIndexAsync(VectorIndexDefinition definition, CancellationToken ct) =>
            Task.CompletedTask;

        public Task UpsertAsync(VectorRecord record, CancellationToken ct)
        {
            UpsertBatches.Add([record]);
            return Task.CompletedTask;
        }

        public Task UpsertManyAsync(
            IReadOnlyCollection<VectorRecord> records,
            CancellationToken ct
        )
        {
            UpsertBatches.Add(records);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            VectorSearchRequest request,
            CancellationToken ct
        ) => Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

        public Task DeleteAsync(
            string indexName,
            IReadOnlyCollection<long> ids,
            CancellationToken ct
        )
        {
            Deletions.Add((indexName, ids));
            return Task.CompletedTask;
        }
    }
}
