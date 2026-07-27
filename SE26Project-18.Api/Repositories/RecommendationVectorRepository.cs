using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Models.VectorProfiles;

namespace SE26Project_18.Api.Repositories;

internal sealed class RecommendationVectorRepository
{
    private const string UserTagVectorFieldName = "user_tag_vector";
    private const string RecruitmentTagVectorFieldName = "recruitment_tag_vector";
    private const string GameTagVectorFieldName = "game_tag_vector";
    private const int VectorDimension = 1536;

    private static readonly VectorIndexDefinition UserIndex = new(
        "user_profiles",
        new[]
        {
            new VectorFieldDefinition(UserTagVectorFieldName, VectorDimension),
            new VectorFieldDefinition(RecruitmentTagVectorFieldName, VectorDimension),
            new VectorFieldDefinition(GameTagVectorFieldName, VectorDimension),
        },
        VectorDistanceMetric.Cosine
    );
    private static readonly VectorIndexDefinition GameIndex = new(
        "game_profiles",
        new[] { new VectorFieldDefinition(GameTagVectorFieldName, VectorDimension) },
        VectorDistanceMetric.Cosine
    );
    private static readonly VectorIndexDefinition RecruitmentIndex = new(
        "recruitment_profiles",
        new[] { new VectorFieldDefinition(RecruitmentTagVectorFieldName, VectorDimension) },
        VectorDistanceMetric.Cosine
    );

    private readonly IVectorStore _vectorStore;

    public RecommendationVectorRepository(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    internal Task EnsureIndexesAsync(CancellationToken ct)
    {
        return Task.WhenAll(
            _vectorStore.EnsureIndexAsync(UserIndex, ct),
            _vectorStore.EnsureIndexAsync(GameIndex, ct),
            _vectorStore.EnsureIndexAsync(RecruitmentIndex, ct)
        );
    }

    public Task UpsertUserProfileAsync(UserVectorProfile profile, CancellationToken ct)
    {
        return _vectorStore.UpsertAsync(
            new VectorRecord(
                UserIndex.Name,
                profile.UserId,
                new Dictionary<string, ReadOnlyMemory<float>>
                {
                    [UserTagVectorFieldName] = profile.UserTagVector,
                    [RecruitmentTagVectorFieldName] = profile.RecruitmentTagVector,
                    [GameTagVectorFieldName] = profile.GameTagVector,
                }
            ),
            ct
        );
    }

    public Task UpsertGameProfileAsync(GameVectorProfile profile, CancellationToken ct)
    {
        return _vectorStore.UpsertAsync(
            new VectorRecord(
                GameIndex.Name,
                profile.GameId,
                new Dictionary<string, ReadOnlyMemory<float>>
                {
                    [GameTagVectorFieldName] = profile.GameTagVector,
                }
            ),
            ct
        );
    }

    public Task UpsertRecruitmentProfileAsync(
        RecruitmentVectorProfile profile,
        CancellationToken ct
    )
    {
        return _vectorStore.UpsertAsync(
            new VectorRecord(
                RecruitmentIndex.Name,
                profile.RecruitmentId,
                new Dictionary<string, ReadOnlyMemory<float>>
                {
                    [RecruitmentTagVectorFieldName] = profile.RecruitmentTagVector,
                }
            ),
            ct
        );
    }

    public Task DeleteUserProfileAsync(long userId, CancellationToken ct)
    {
        return _vectorStore.DeleteAsync(UserIndex.Name, new[] { userId }, ct);
    }

    public Task DeleteGameProfileAsync(long gameId, CancellationToken ct)
    {
        return _vectorStore.DeleteAsync(GameIndex.Name, new[] { gameId }, ct);
    }

    public Task DeleteRecruitmentProfileAsync(long recruitmentId, CancellationToken ct)
    {
        return _vectorStore.DeleteAsync(RecruitmentIndex.Name, new[] { recruitmentId }, ct);
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByUserTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return SearchAsync(UserIndex.Name, UserTagVectorFieldName, queryVector, limit, ct);
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return SearchAsync(UserIndex.Name, RecruitmentTagVectorFieldName, queryVector, limit, ct);
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return SearchAsync(UserIndex.Name, GameTagVectorFieldName, queryVector, limit, ct);
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchGamesByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return SearchAsync(GameIndex.Name, GameTagVectorFieldName, queryVector, limit, ct);
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchRecruitmentsByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return SearchAsync(
            RecruitmentIndex.Name,
            RecruitmentTagVectorFieldName,
            queryVector,
            limit,
            ct
        );
    }

    private Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string indexName,
        string vectorFieldName,
        ReadOnlyMemory<float> queryVector,
        int limit,
        CancellationToken ct
    )
    {
        return _vectorStore.SearchAsync(
            new VectorSearchRequest(indexName, vectorFieldName, queryVector, limit),
            ct
        );
    }
}
