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

    private readonly int _batchSize;

    public RecommendationVectorRepository(
        IVectorStore vectorStore,
        IOptions<EmbeddingOptions> options,
        IOptions<EmbeddingSyncOptions> syncOptions
    )
    {
        _vectorStore = vectorStore;
        _batchSize = syncOptions.Value.MilvusBatchSize;
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
        return SynchronizeUserProfilesAsync([profile], ct);
    }

    public Task UpsertGameProfileAsync(GameVectorProfile profile, CancellationToken ct)
    {
        return SynchronizeGameProfilesAsync([profile], ct);
    }

    public Task UpsertRecruitmentProfileAsync(
        RecruitmentVectorProfile profile,
        CancellationToken ct
    )
    {
        return SynchronizeRecruitmentProfilesAsync([profile], ct);
    }

    public Task SynchronizeUserProfilesAsync(
        IReadOnlyCollection<UserVectorProfile> profiles,
        CancellationToken ct
    )
    {
        return Task.WhenAll(
            SynchronizeIndexAsync(
                _userOwnTagIndex,
                profiles,
                profile => profile.UserId,
                profile => profile.OwnUserTagVector,
                ct
            ),
            SynchronizeIndexAsync(
                _userInterestedTagIndex,
                profiles,
                profile => profile.UserId,
                profile => profile.InterestedUserTagVector,
                ct
            ),
            SynchronizeIndexAsync(
                _userRecruitmentTagIndex,
                profiles,
                profile => profile.UserId,
                profile => profile.RecruitmentTagVector,
                ct
            ),
            SynchronizeIndexAsync(
                _userGameTagIndex,
                profiles,
                profile => profile.UserId,
                profile => profile.GameTagVector,
                ct
            )
        );
    }

    public Task SynchronizeGameProfilesAsync(
        IReadOnlyCollection<GameVectorProfile> profiles,
        CancellationToken ct
    )
    {
        return SynchronizeIndexAsync(
            _gameIndex,
            profiles,
            profile => profile.GameId,
            profile => profile.GameTagVector,
            ct
        );
    }

    public Task SynchronizeRecruitmentProfilesAsync(
        IReadOnlyCollection<RecruitmentVectorProfile> profiles,
        CancellationToken ct
    )
    {
        return SynchronizeIndexAsync(
            _recruitmentIndex,
            profiles,
            profile => profile.RecruitmentId,
            profile => profile.RecruitmentTagVector,
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
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_userOwnTagIndex, queryVector, allowedIds, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByInterestedTagAsync(
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_userInterestedTagIndex, queryVector, allowedIds, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_userRecruitmentTagIndex, queryVector, allowedIds, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchUsersByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_userGameTagIndex, queryVector, allowedIds, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchGamesByGameTagAsync(
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_gameIndex, queryVector, allowedIds, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchRecruitmentsByRecruitmentTagAsync(
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    ) => SearchAsync(_recruitmentIndex, queryVector, allowedIds, ct);

    private static VectorIndexDefinition CreateIndex(string name, string fieldName, int dimension)
    {
        return new VectorIndexDefinition(
            name,
            new[] { new VectorFieldDefinition(fieldName, dimension) },
            VectorDistanceMetric.Cosine
        );
    }

    private async Task SynchronizeIndexAsync<TProfile>(
        VectorIndexDefinition index,
        IReadOnlyCollection<TProfile> profiles,
        Func<TProfile, long> getId,
        Func<TProfile, ReadOnlyMemory<float>?> getVector,
        CancellationToken ct
    )
    {
        var fieldName = index.Fields.Single().Name;
        var upserts = profiles
            .Where(profile => getVector(profile).HasValue)
            .Select(profile => new VectorRecord(
                index.Name,
                getId(profile),
                new Dictionary<string, ReadOnlyMemory<float>>
                {
                    [fieldName] = getVector(profile)!.Value,
                }
            ))
            .ToList();
        var deletes = profiles
            .Where(profile => !getVector(profile).HasValue)
            .Select(getId)
            .Distinct()
            .ToList();

        foreach (var batch in upserts.Chunk(_batchSize))
        {
            await _vectorStore.UpsertManyAsync(batch, ct);
        }
        foreach (var batch in deletes.Chunk(_batchSize))
        {
            await _vectorStore.DeleteAsync(index.Name, batch, ct);
        }
    }

    private async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorIndexDefinition index,
        ReadOnlyMemory<float> queryVector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct
    )
    {
        var ids = allowedIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var tasks = ids.Chunk(1_000)
            .Select(chunk =>
                _vectorStore.SearchAsync(
                    new VectorSearchRequest(
                        index.Name,
                        index.Fields.Single().Name,
                        queryVector,
                        chunk.Length,
                        chunk
                    ),
                    ct
                )
            );
        var results = await Task.WhenAll(tasks);
        return results
            .SelectMany(result => result)
            .OrderByDescending(result => result.Score)
            .ToList();
    }

    private Task DeleteAsync(VectorIndexDefinition index, long id, CancellationToken ct)
    {
        return _vectorStore.DeleteAsync(index.Name, new[] { id }, ct);
    }
}
