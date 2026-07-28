using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Repositories;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services.Recommendations;

public sealed class EmbeddingRecruitmentRecommendationAlgorithmTests
{
    [Fact]
    public async Task RankAsync_OrdersCandidatesByRecruitmentTagSimilarity()
    {
        var vectorStore = new StubVectorStore(
            [new VectorSearchResult(2, 1f), new VectorSearchResult(1, 0f)]
        );
        var repository = new RecommendationVectorRepository(
            vectorStore,
            Options.Create(new EmbeddingOptions { Dimension = 2 })
        );
        var algorithm = new EmbeddingRecruitmentRecommendationAlgorithm(
            repository,
            new StubProfileBuilder(
                new UserPreferenceProfile(null, null, new float[] { 1f, 0f }, null)
            ),
            NullLogger<EmbeddingRecruitmentRecommendationAlgorithm>.Instance
        );
        var candidates = new[]
        {
            new RecruitmentRecommendationCandidate(1, 10, 100),
            new RecruitmentRecommendationCandidate(2, 20, 200),
        };

        var result = await algorithm.RankAsync(7, candidates, CancellationToken.None);

        Assert.Equal([2L, 1L], result);
    }

    private sealed class StubProfileBuilder(UserPreferenceProfile profile)
        : IUserPreferenceProfileBuilder
    {
        public Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct)
        {
            return Task.FromResult(profile);
        }
    }

    private sealed class StubVectorStore(IReadOnlyList<VectorSearchResult> results) : IVectorStore
    {
        public Task EnsureIndexAsync(VectorIndexDefinition definition, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task UpsertAsync(VectorRecord record, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            VectorSearchRequest request,
            CancellationToken ct
        )
        {
            return Task.FromResult(results);
        }

        public Task DeleteAsync(
            string indexName,
            IReadOnlyCollection<long> ids,
            CancellationToken ct
        )
        {
            return Task.CompletedTask;
        }
    }
}
