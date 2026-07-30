using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Infrastructure.VectorStore;
using SE26Project_18.Backend.Repositories;

namespace SE26Project_18.Backend.Tests.Recommendations;

public sealed class MilvusBootstrapTests
{
    [Fact]
    public async Task EnsureIndexes_CreatesMissingConfiguredDatabase()
    {
        if (Environment.GetEnvironmentVariable("RUN_MILVUS_INTEGRATION") != "1")
            return;

        using var store = new MilvusVectorStore(
            Options.Create(new MilvusOptions
            {
                HostName = "localhost",
                Port = 19530,
                DatabaseName = "se26project_18",
            }),
            NullLogger<MilvusVectorStore>.Instance);
        var repository = new RecommendationVectorRepository(
            store,
            Options.Create(new EmbeddingOptions { Dimension = 1024 }),
            Options.Create(new EmbeddingSyncOptions { MilvusBatchSize = 100 }));

        await repository.EnsureIndexesAsync(default);
    }
}
