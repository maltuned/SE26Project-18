using Microsoft.Extensions.Options;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Infrastructure.VectorStore;
using SE26Project_18.Backend.Models.VectorProfiles;

namespace SE26Project_18.Backend.Repositories;

internal sealed class RecommendationVectorRepository
{
    private readonly IVectorStore _store;
    private readonly VectorIndexDefinition _userRecruitmentIndex;
    private readonly VectorIndexDefinition _userGameIndex;
    private readonly VectorIndexDefinition _gameIndex;
    private readonly VectorIndexDefinition _recruitmentIndex;
    private readonly int _batchSize;

    public RecommendationVectorRepository(
        IVectorStore store,
        IOptions<EmbeddingOptions> embedding,
        IOptions<EmbeddingSyncOptions> sync)
    {
        _store = store;
        _batchSize = sync.Value.MilvusBatchSize;
        var dimension = embedding.Value.Dimension;
        _userRecruitmentIndex = Create("user_recruitment_tag_profiles", "recruitment_tag_vector", dimension);
        _userGameIndex = Create("user_game_tag_profiles", "game_tag_vector", dimension);
        _gameIndex = Create("game_profiles", "game_tag_vector", dimension);
        _recruitmentIndex = Create("recruitment_profiles", "recruitment_tag_vector", dimension);
    }

    internal Task EnsureIndexesAsync(CancellationToken ct) => Task.WhenAll(
        _store.EnsureIndexAsync(_userRecruitmentIndex, ct),
        _store.EnsureIndexAsync(_userGameIndex, ct),
        _store.EnsureIndexAsync(_gameIndex, ct),
        _store.EnsureIndexAsync(_recruitmentIndex, ct));

    public Task SynchronizeUserProfilesAsync(IReadOnlyCollection<UserVectorProfile> profiles, CancellationToken ct) =>
        Task.WhenAll(
            SynchronizeAsync(_userRecruitmentIndex, profiles, item => item.UserId, item => item.RecruitmentTagVector, ct),
            SynchronizeAsync(_userGameIndex, profiles, item => item.UserId, item => item.GameTagVector, ct));

    public Task SynchronizeGameProfilesAsync(IReadOnlyCollection<GameVectorProfile> profiles, CancellationToken ct) =>
        SynchronizeAsync(_gameIndex, profiles, item => item.GameId, item => item.GameTagVector, ct);

    public Task SynchronizeRecruitmentProfilesAsync(
        IReadOnlyCollection<RecruitmentVectorProfile> profiles, CancellationToken ct) =>
        SynchronizeAsync(_recruitmentIndex, profiles, item => item.RecruitmentId, item => item.RecruitmentTagVector, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchGamesByGameTagAsync(
        ReadOnlyMemory<float> vector, IReadOnlyCollection<long> ids, CancellationToken ct) =>
        SearchAsync(_gameIndex, vector, ids, ct);

    public Task<IReadOnlyList<VectorSearchResult>> SearchRecruitmentsByRecruitmentTagAsync(
        ReadOnlyMemory<float> vector, IReadOnlyCollection<long> ids, CancellationToken ct) =>
        SearchAsync(_recruitmentIndex, vector, ids, ct);

    private static VectorIndexDefinition Create(string name, string field, int dimension) =>
        new(name, [new VectorFieldDefinition(field, dimension)], VectorDistanceMetric.Cosine);

    private async Task SynchronizeAsync<T>(
        VectorIndexDefinition index,
        IReadOnlyCollection<T> profiles,
        Func<T, long> id,
        Func<T, ReadOnlyMemory<float>?> vector,
        CancellationToken ct)
    {
        var field = index.Fields.Single().Name;
        var upserts = profiles.Where(item => vector(item).HasValue)
            .Select(item => new VectorRecord(index.Name, id(item),
                new Dictionary<string, ReadOnlyMemory<float>> { [field] = vector(item)!.Value }))
            .ToList();
        var deletes = profiles.Where(item => !vector(item).HasValue).Select(id).Distinct().ToList();
        foreach (var batch in upserts.Chunk(_batchSize)) await _store.UpsertManyAsync(batch, ct);
        foreach (var batch in deletes.Chunk(_batchSize)) await _store.DeleteAsync(index.Name, batch, ct);
    }

    private async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorIndexDefinition index,
        ReadOnlyMemory<float> vector,
        IReadOnlyCollection<long> allowedIds,
        CancellationToken ct)
    {
        var ids = allowedIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var tasks = ids.Chunk(1_000).Select(chunk => _store.SearchAsync(
            new VectorSearchRequest(index.Name, index.Fields.Single().Name, vector, chunk.Length, chunk), ct));
        return (await Task.WhenAll(tasks)).SelectMany(item => item)
            .OrderByDescending(item => item.Score).ToList();
    }
}
