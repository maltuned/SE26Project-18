using Microsoft.Extensions.Options;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.VectorProfiles;

namespace SE26Project_18.Api.Repositories;

internal sealed class RecommendationVectorRepository
{
    private readonly IVectorStore _vectorStore;
    private readonly VectorIndexDefinition _userOwnTagIndex;
    private readonly VectorIndexDefinition _userInterestedTagIndex;
    private readonly VectorIndexDefinition _userRecruitmentTagIndex;
    private readonly VectorIndexDefinition _userGameTagIndex;
    private readonly VectorIndexDefinition _gameIndex;
    private readonly VectorIndexDefinition _recruitmentIndex;

    public RecommendationVectorRepository(
        IVectorStore vectorStore,
        IOptions<EmbeddingOptions> options
    )
    {
        _vectorStore = vectorStore;
        var dimension = options.Value.Dimension;
        _userOwnTagIndex = CreateIndex("user_own_tag_profiles", "user_tag_vector", dimension);
        _userInterestedTagIndex = CreateIndex(
            "user_interested_tag_profiles",
            "interested_user_tag_vector",
            dimension
        );
        _userRecruitmentTagIndex = CreateIndex(
            "user_recruitment_tag_profiles",
            "recruitment_tag_vector",
            dimension
        );
        _userGameTagIndex = CreateIndex("user_game_tag_profiles", "game_tag_vector", dimension);
        _gameIndex = CreateIndex("game_profiles", "game_tag_vector", dimension);
        _recruitmentIndex = CreateIndex(
            "recruitment_profiles",
            "recruitment_tag_vector",
            dimension
        );
    }

    internal Task EnsureIndexesAsync(CancellationToken ct)
    {
        return Task.WhenAll(
            _vectorStore.EnsureIndexAsync(_userOwnTagIndex, ct),
            _vectorStore.EnsureIndexAsync(_userInterestedTagIndex, ct),
            _vectorStore.EnsureIndexAsync(_userRecruitmentTagIndex, ct),
            _vectorStore.EnsureIndexAsync(_userGameTagIndex, ct),
            _vectorStore.EnsureIndexAsync(_gameIndex, ct),
            _vectorStore.EnsureIndexAsync(_recruitmentIndex, ct)
        );
    }

    public Task UpsertUserProfileAsync(UserVectorProfile profile, CancellationToken ct)
    {
        var operations = new List<Task>(4);
        AddUpsert(operations, _userOwnTagIndex, profile.UserId, profile.OwnUserTagVector, ct);
        AddUpsert(
            operations,
            _userInterestedTagIndex,
            profile.UserId,
            profile.InterestedUserTagVector,
            ct
        );
        AddUpsert(
            operations,
            _userRecruitmentTagIndex,
            profile.UserId,
            profile.RecruitmentTagVector,
            ct
        );
        AddUpsert(operations, _userGameTagIndex, profile.UserId, profile.GameTagVector, ct);
        return Task.WhenAll(operations);
    }

    public Task UpsertGameProfileAsync(GameVectorProfile profile, CancellationToken ct)
    {
        return UpsertAsync(_gameIndex, profile.GameId, profile.GameTagVector, ct);
    }

    public Task UpsertRecruitmentProfileAsync(
        RecruitmentVectorProfile profile,
        CancellationToken ct
    )
    {
        return UpsertAsync(
            _recruitmentIndex,
            profile.RecruitmentId,
            profile.RecruitmentTagVector,
            ct
        );
    }

    public Task DeleteUserProfileAsync(long userId, CancellationToken ct)
    {
        return Task.WhenAll(
            DeleteAsync(_userOwnTagIndex, userId, ct),
            DeleteAsync(_userInterestedTagIndex, userId, ct),
            DeleteAsync(_userRecruitmentTagIndex, userId, ct),
            DeleteAsync(_userGameTagIndex, userId, ct)
        );
    }

    public Task DeleteGameProfileAsync(long gameId, CancellationToken ct) =>
        DeleteAsync(_gameIndex, gameId, ct);

    public Task DeleteRecruitmentProfileAsync(long recruitmentId, CancellationToken ct) =>
        DeleteAsync(_recruitmentIndex, recruitmentId, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByOwnTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_userOwnTagIndex, queryVector, limit, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByInterestedTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_userInterestedTagIndex, queryVector, limit, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_userRecruitmentTagIndex, queryVector, limit, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_userGameTagIndex, queryVector, limit, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchGamesByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_gameIndex, queryVector, limit, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchRecruitmentsByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    ) => SearchAsync(_recruitmentIndex, queryVector, limit, ct);

    private static VectorIndexDefinition CreateIndex(string name, string fieldName, int dimension)
    {
        return new VectorIndexDefinition(
            name,
            new[] { new VectorFieldDefinition(fieldName, dimension) },
            VectorDistanceMetric.Cosine
        );
    }

    private void AddUpsert(
        ICollection<Task> operations,
        VectorIndexDefinition index,
        long id,
        ReadOnlyMemory<float>? vector,
        CancellationToken ct
    )
    {
        if (vector.HasValue)
            operations.Add(UpsertAsync(index, id, vector.Value, ct));
    }

    private Task UpsertAsync(
        VectorIndexDefinition index,
        long id,
        ReadOnlyMemory<float> vector,
        CancellationToken ct
    )
    {
        return _vectorStore.UpsertAsync(
            new VectorRecord(
                index.Name,
                id,
                new Dictionary<string, ReadOnlyMemory<float>>
                {
                    [index.Fields.Single().Name] = vector,
                }
            ),
            ct
        );
    }

    private Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorIndexDefinition index,
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return _vectorStore.SearchAsync(
            new VectorSearchRequest(index.Name, index.Fields.Single().Name, queryVector, limit),
            ct
        );
    }

    private Task DeleteAsync(VectorIndexDefinition index, long id, CancellationToken ct)
    {
        return _vectorStore.DeleteAsync(index.Name, new[] { id }, ct);
    }
}
