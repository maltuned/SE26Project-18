using Milvus.Client;
using SE26Project_18.Api.Infrastructure.VectorStore;

namespace SE26Project_18.Api.Tests.Infrastructure;

public sealed class MilvusVectorStoreTests
{
    [Fact]
    public void MapSearchResults_TreatsMissingIdsAndScoresAsNoMatches()
    {
        var result = CreateResult([]);

        var matches = MilvusVectorStore.MapSearchResults("recruitment_profiles", result);

        Assert.Empty(matches);
    }

    [Fact]
    public void MapSearchResults_RejectsMissingIdsWhenScoresWereReturned()
    {
        var result = CreateResult([0.5f]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            MilvusVectorStore.MapSearchResults("recruitment_profiles", result)
        );

        Assert.Contains("did not return Int64 primary keys", exception.Message);
    }

    private static SearchResults CreateResult(IReadOnlyList<float> scores)
    {
        return new SearchResults
        {
            CollectionName = "recruitment_profiles",
            FieldsData = [],
            Ids = default,
            NumQueries = 1,
            Scores = scores,
            Limit = 20,
            Limits = [scores.Count],
        };
    }
}
