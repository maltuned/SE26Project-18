using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Exceptions;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Infrastructure.VectorStore;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Models.Recommendations;
using SE26Project_18.Backend.Models.VectorProfiles;
using SE26Project_18.Backend.Repositories;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Tests.Recommendations;

public sealed class RecommendationCoverageTests
{
    [Fact]
    public async Task VectorRepository_EnsuresAllIndexesWithConfiguredDimension()
    {
        var store = new RecordingVectorStore();
        var repository = CreateRepository(store, dimension: 7);

        await repository.EnsureIndexesAsync(default);

        Assert.Equal(
            ["game_profiles", "recruitment_profiles", "user_game_tag_profiles", "user_recruitment_tag_profiles"],
            store.Indexes.Select(index => index.Name).Order().ToArray());
        Assert.All(store.Indexes, index =>
        {
            Assert.Equal(VectorDistanceMetric.Cosine, index.Metric);
            Assert.Equal(7, Assert.Single(index.Fields).Dimension);
        });
    }

    [Fact]
    public async Task VectorRepository_SynchronizesUpsertsAndDeletesInConfiguredBatches()
    {
        var store = new RecordingVectorStore();
        var repository = CreateRepository(store, batchSize: 2);
        ReadOnlyMemory<float> vector = new float[] { 1f, 0f };

        await repository.SynchronizeUserProfilesAsync(
            [
                new UserVectorProfile(1, vector, vector),
                new UserVectorProfile(2, vector, null),
                new UserVectorProfile(3, vector, null),
                new UserVectorProfile(4, null, null),
            ], default);
        await repository.SynchronizeGameProfilesAsync(
            [new GameVectorProfile(10, vector), new GameVectorProfile(11, null)], default);
        await repository.SynchronizeRecruitmentProfilesAsync(
            [new RecruitmentVectorProfile(20, vector), new RecruitmentVectorProfile(21, null)], default);

        var userRecruitmentUpserts = store.Upserts
            .Where(batch => batch[0].IndexName == "user_recruitment_tag_profiles").ToList();
        Assert.Equal([2, 1], userRecruitmentUpserts.Select(batch => batch.Count).ToArray());
        Assert.Equal([1L, 2L, 3L], userRecruitmentUpserts.SelectMany(batch => batch).Select(item => item.Id).ToArray());
        Assert.All(userRecruitmentUpserts.SelectMany(batch => batch), record =>
            Assert.Equal(new float[] { 1f, 0f }, record.Vectors["recruitment_tag_vector"].ToArray()));

        Assert.Contains(store.Upserts, batch =>
            batch.Count == 1 && batch[0].IndexName == "user_game_tag_profiles" && batch[0].Id == 1);
        Assert.Contains(store.Upserts, batch =>
            batch.Count == 1 && batch[0].IndexName == "game_profiles" && batch[0].Id == 10);
        Assert.Contains(store.Upserts, batch =>
            batch.Count == 1 && batch[0].IndexName == "recruitment_profiles" && batch[0].Id == 20);

        AssertDelete(store, "user_recruitment_tag_profiles", [4]);
        AssertDelete(store, "user_game_tag_profiles", [2, 3]);
        AssertDelete(store, "user_game_tag_profiles", [4]);
        AssertDelete(store, "game_profiles", [11]);
        AssertDelete(store, "recruitment_profiles", [21]);
    }

    [Fact]
    public async Task VectorRepository_SearchesDistinctIdsInChunksAndGloballySortsResults()
    {
        var store = new RecordingVectorStore
        {
            SearchHandler = request =>
            {
                var allowedIds = request.AllowedIds!;
                var id = allowedIds.Contains(1001) ? 1001L : allowedIds.First();
                var score = id == 1001 ? 0.9f : 0.1f;
                return [new VectorSearchResult(id, score)];
            },
        };
        var repository = CreateRepository(store);

        var empty = await repository.SearchGamesByGameTagAsync(new float[] { 1f, 0f }, [], default);
        var ids = Enumerable.Range(1, 1002).Select(value => (long)value).Append(1).ToArray();
        var results = await repository.SearchRecruitmentsByRecruitmentTagAsync(
            new float[] { 0f, 1f }, ids, default);

        Assert.Empty(empty);
        Assert.Equal([1001L, 1L], results.Select(result => result.Id).ToArray());
        Assert.Equal([2, 1000], store.Searches.Select(request => request.Limit).Order().ToArray());
        Assert.All(store.Searches, request =>
        {
            Assert.Equal("recruitment_profiles", request.IndexName);
            Assert.Equal("recruitment_tag_vector", request.VectorFieldName);
            Assert.Equal(request.Limit, request.AllowedIds!.Count);
        });
    }

    [Fact]
    public async Task RecommendationAlgorithm_EmptyCandidatesDoesNotBuildProfile()
    {
        var profileBuilder = new StubProfileBuilder(new UserPreferenceProfile(null, null));
        var algorithm = new EmbeddingRecruitmentRecommendationAlgorithm(
            CreateRepository(new RecordingVectorStore()), profileBuilder);

        var result = await algorithm.RankAsync(42, [], default);

        Assert.Empty(result);
        Assert.Equal(0, profileBuilder.BuildCalls);
    }

    [Fact]
    public async Task RecommendationAlgorithm_NoPreferenceVectorsPreservesOriginalOrder()
    {
        var profileBuilder = new StubProfileBuilder(new UserPreferenceProfile(null, null));
        var store = new RecordingVectorStore();
        var algorithm = new EmbeddingRecruitmentRecommendationAlgorithm(
            CreateRepository(store), profileBuilder);
        RecruitmentRecommendationCandidate[] candidates =
        [
            new(3, 30),
            new(1, null),
            new(2, 20),
        ];

        var result = await algorithm.RankAsync(42, candidates, default);

        Assert.Equal([3L, 1L, 2L], result);
        Assert.Empty(store.Searches);
        Assert.Equal(1, profileBuilder.BuildCalls);
    }

    [Fact]
    public async Task RecommendationAlgorithm_CombinesAvailableRecruitmentAndGameScores()
    {
        var store = new RecordingVectorStore
        {
            SearchHandler = request => request.IndexName switch
            {
                "recruitment_profiles" =>
                [
                    new VectorSearchResult(1, -1f),
                    new VectorSearchResult(2, 0.2f),
                    new VectorSearchResult(3, 0.1f),
                ],
                "game_profiles" =>
                [new VectorSearchResult(10, 1f), new VectorSearchResult(20, -1f)],
                _ => throw new InvalidOperationException("Unexpected index."),
            },
        };
        var profileBuilder = new StubProfileBuilder(new UserPreferenceProfile(
            new float[] { 1f, 0f }, new float[] { 0f, 1f }));
        var algorithm = new EmbeddingRecruitmentRecommendationAlgorithm(
            CreateRepository(store), profileBuilder);

        var result = await algorithm.RankAsync(42,
            [new(1, 10), new(2, 20), new(3, null)], default);

        Assert.Equal([3L, 2L, 1L], result);
        Assert.Equal(2, store.Searches.Count);
        Assert.Equal([1L, 2L, 3L], store.Searches.Single(request =>
            request.IndexName == "recruitment_profiles").AllowedIds);
        Assert.Equal([10L, 20L], store.Searches.Single(request =>
            request.IndexName == "game_profiles").AllowedIds);
    }

    [Fact]
    public async Task UserPreferenceProfileBuilder_ThrowsForUnknownUser()
    {
        await using var db = CreateDbContext();
        var builder = CreateProfileBuilder(db, new RecordingEmbeddingService());
        var service = new UserPreferenceProfileBuilder(db, builder);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.BuildAsync(999, default));

        Assert.Equal("User not found.", exception.Message);
    }

    [Fact]
    public async Task UserPreferenceProfileBuilder_ReturnsBuiltVectorsForExistingUser()
    {
        await using var db = CreateDbContext();
        var user = new User("publisher", "hash");
        var recruitmentTag = new RecruitmentTag("Strategy");
        var gameTag = new GameTag("RPG");
        var recruitment = new Recruitment("team", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = user,
            RecruitmentTags = [recruitmentTag],
            GameTags = [gameTag],
        };
        db.AddRange(user, recruitment);
        await db.SaveChangesAsync();
        var service = new UserPreferenceProfileBuilder(
            db, CreateProfileBuilder(db, CreateCategoryEmbeddingService()));

        var profile = await service.BuildAsync(user.Id, default);

        Assert.Equal(new float[] { 1f, 0f }, profile.RecruitmentTagVector!.Value.ToArray());
        Assert.Equal(new float[] { 0f, 1f }, profile.GameTagVector!.Value.ToArray());
    }

    [Fact]
    public async Task ProfileBatchBuilder_BuildsBehaviorGameAndRecruitmentProfilesIncludingMissingEntities()
    {
        await using var db = CreateDbContext();
        var publisher = new User("publisher", "hash");
        var responder = new User("responder", "hash");
        var viewer = new User("viewer", "hash");
        var gameTag = new GameTag("RPG");
        var recruitmentTag = new RecruitmentTag("Strategy");
        var game = new Game("game") { Tags = [gameTag] };
        var open = new Recruitment("open", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = publisher,
            Game = game,
            GameTags = [gameTag],
            RecruitmentTags = [recruitmentTag],
        };
        var deleted = new Recruitment("deleted", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = publisher,
            Status = RecruitmentStatus.Deleted,
            RecruitmentTags = [recruitmentTag],
        };
        db.AddRange(publisher, responder, viewer, game, open, deleted);
        await db.SaveChangesAsync();
        db.Responses.Add(new Response
        {
            Recruitment = open,
            Responser = responder,
            RecruitmentId = open.Id,
            ResponserId = responder.Id,
        });
        var view = new RecruitmentView(viewer, open);
        view.RecordView();
        db.RecruitmentViews.Add(view);
        await db.SaveChangesAsync();
        var builder = CreateProfileBuilder(db, CreateCategoryEmbeddingService());

        var users = await builder.BuildUsersAsync(
            [publisher.Id, responder.Id, viewer.Id, 999, publisher.Id], default);
        var games = await builder.BuildGamesAsync([game.Id, 998, game.Id], default);
        var recruitments = await builder.BuildRecruitmentsAsync(
            [open.Id, deleted.Id, 997, open.Id], default);

        Assert.Equal(4, users.Count);
        foreach (var id in new[] { publisher.Id, responder.Id, viewer.Id })
        {
            var profile = users.Single(item => item.UserId == id);
            Assert.Equal(new float[] { 1f, 0f }, profile.RecruitmentTagVector!.Value.ToArray());
            Assert.Equal(new float[] { 0f, 1f }, profile.GameTagVector!.Value.ToArray());
        }
        var missingUser = users.Single(item => item.UserId == 999);
        Assert.Null(missingUser.RecruitmentTagVector);
        Assert.Null(missingUser.GameTagVector);
        Assert.Equal(new float[] { 0f, 1f }, games.Single(item => item.GameId == game.Id)
            .GameTagVector!.Value.ToArray());
        Assert.Null(games.Single(item => item.GameId == 998).GameTagVector);
        Assert.Equal(new float[] { 1f, 0f }, recruitments.Single(item => item.RecruitmentId == open.Id)
            .RecruitmentTagVector!.Value.ToArray());
        Assert.Null(recruitments.Single(item => item.RecruitmentId == deleted.Id).RecruitmentTagVector);
        Assert.Null(recruitments.Single(item => item.RecruitmentId == 997).RecruitmentTagVector);
    }

    [Fact]
    public async Task TagEmbeddingBuilder_DeduplicatesTextsAndComputesWeightedNormalizedProfiles()
    {
        var embeddings = new RecordingEmbeddingService(text => text.EndsWith("Alpha")
            ? new float[] { 1f, 0f }
            : new float[] { 0f, 1f });
        var builder = new TagEmbeddingBuilder(
            embeddings, Options.Create(new EmbeddingOptions { Dimension = 2 }));
        IReadOnlyDictionary<long, IReadOnlyCollection<WeightedTagInput>> profiles =
            new Dictionary<long, IReadOnlyCollection<WeightedTagInput>>
            {
                [1] = [new(1, "Alpha", 3d), new(2, "Beta", 1d)],
                [2] = [new(1, "Alpha", 1d)],
                [3] = [],
            };

        var result = await builder.BuildManyAsync(profiles, "game tag", default);

        Assert.Equal(["game tag: Alpha", "game tag: Beta"], Assert.Single(embeddings.Requests));
        var weighted = result[1]!.Value.ToArray();
        Assert.Equal(0.94868f, weighted[0], 5);
        Assert.Equal(0.31623f, weighted[1], 5);
        Assert.Equal(new float[] { 1f, 0f }, result[2]!.Value.ToArray());
        Assert.Null(result[3]);
    }

    [Fact]
    public async Task TagEmbeddingBuilder_EmptySingleProfileSkipsEmbeddingRequest()
    {
        var embeddings = new RecordingEmbeddingService();
        var builder = new TagEmbeddingBuilder(
            embeddings, Options.Create(new EmbeddingOptions { Dimension = 2 }));

        var result = await builder.BuildAsync([], "game tag", default);

        Assert.Null(result);
        Assert.Empty(embeddings.Requests);
    }

    [Theory]
    [InlineData((EmbeddingTarget)99, 1, 1)]
    [InlineData(EmbeddingTarget.User, 0, 1)]
    [InlineData(EmbeddingTarget.User, 1, 0)]
    public async Task SyncConsumer_RejectsInvalidEvents(
        EmbeddingTarget target, long entityId, long version)
    {
        await using var db = CreateDbContext();
        var consumer = CreateConsumer(db, new RecordingVectorStore());
        var message = new EmbeddingSyncRequested(Guid.Empty, target, entityId, version);

        await Assert.ThrowsAsync<EmbeddingSyncValidationException>(
            () => consumer.ConsumeAsync([message], default));
    }

    [Fact]
    public async Task SyncConsumer_DeduplicatesNewestEventsSynchronizesAllTargetsAndAppliesVersions()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash");
        var game = new Game("game");
        var recruitment = new Recruitment("team", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = user,
        };
        db.AddRange(user, game, recruitment);
        await db.SaveChangesAsync();
        var store = new RecordingVectorStore();
        var consumer = CreateConsumer(db, store);
        EmbeddingSyncRequested[] events =
        [
            new(Guid.Parse("00000000-0000-0000-0000-000000000001"), EmbeddingTarget.User, user.Id, 2),
            new(Guid.Parse("00000000-0000-0000-0000-000000000002"), EmbeddingTarget.User, user.Id, 5),
            new(Guid.Parse("00000000-0000-0000-0000-000000000003"), EmbeddingTarget.Game, game.Id, 3),
            new(Guid.Parse("00000000-0000-0000-0000-000000000004"), EmbeddingTarget.Recruitment, recruitment.Id, 4),
            new(Guid.Parse("00000000-0000-0000-0000-000000000005"), EmbeddingTarget.Game, 999, 1),
        ];

        await consumer.ConsumeAsync(events, default);

        Assert.Equal(5, user.AppliedEmbeddingVersion);
        Assert.Equal(3, game.AppliedEmbeddingVersion);
        Assert.Equal(4, recruitment.AppliedEmbeddingVersion);
        AssertDelete(store, "user_recruitment_tag_profiles", [user.Id]);
        AssertDelete(store, "user_game_tag_profiles", [user.Id]);
        AssertDelete(store, "game_profiles", [game.Id, 999]);
        AssertDelete(store, "recruitment_profiles", [recruitment.Id]);
    }

    [Fact]
    public async Task SyncScheduler_IgnoresInvalidAndDuplicateIdsButKeepsDifferentTargets()
    {
        await using var db = CreateDbContext();
        var scheduler = new EmbeddingSyncScheduler(db);

        scheduler.Schedule(EmbeddingTarget.User, 0);
        scheduler.Schedule(EmbeddingTarget.User, [7, 7, 0, 8]);
        scheduler.Schedule(EmbeddingTarget.User, 7);
        scheduler.Schedule(EmbeddingTarget.Game, 7);
        await db.SaveChangesAsync();

        var messages = await db.EmbeddingSyncOutbox.OrderBy(item => item.Target)
            .ThenBy(item => item.EntityId).ToListAsync();
        Assert.Equal(3, messages.Count);
        Assert.Contains(messages, item => item.Target == EmbeddingTarget.User && item.EntityId == 7);
        Assert.Contains(messages, item => item.Target == EmbeddingTarget.User && item.EntityId == 8);
        Assert.Contains(messages, item => item.Target == EmbeddingTarget.Game && item.EntityId == 7);
    }

    private static RecommendationVectorRepository CreateRepository(
        IVectorStore store, int dimension = 2, int batchSize = 100) =>
        new(store,
            Options.Create(new EmbeddingOptions { Dimension = dimension }),
            Options.Create(new EmbeddingSyncOptions { MilvusBatchSize = batchSize }));

    private static AppDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"recommendation-coverage-{Guid.NewGuid():N}")
            .Options);

    private static EmbeddingProfileBatchBuilder CreateProfileBuilder(
        AppDbContext db, IEmbeddingService embeddingService) =>
        new(db, new TagEmbeddingBuilder(
            embeddingService, Options.Create(new EmbeddingOptions { Dimension = 2 })));

    private static RecordingEmbeddingService CreateCategoryEmbeddingService() => new(text =>
        text.StartsWith("recruitment tag:", StringComparison.Ordinal)
            ? new float[] { 1f, 0f }
            : new float[] { 0f, 1f });

    private static EmbeddingSyncBatchConsumer CreateConsumer(
        AppDbContext db, RecordingVectorStore store) =>
        new(CreateProfileBuilder(db, CreateCategoryEmbeddingService()), CreateRepository(store), db);

    private static void AssertDelete(
        RecordingVectorStore store, string indexName, IReadOnlyCollection<long> expectedIds)
    {
        Assert.Contains(store.Deletes, call =>
            call.IndexName == indexName && call.Ids.SequenceEqual(expectedIds));
    }

    private sealed class StubProfileBuilder(UserPreferenceProfile profile) : IUserPreferenceProfileBuilder
    {
        public int BuildCalls { get; private set; }

        public Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct)
        {
            BuildCalls++;
            return Task.FromResult(profile);
        }
    }

    private sealed class RecordingEmbeddingService(
        Func<string, ReadOnlyMemory<float>>? vectorFactory = null) : IEmbeddingService
    {
        public List<IReadOnlyCollection<string>> Requests { get; } = [];

        public Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyCollection<string> texts, CancellationToken ct)
        {
            Requests.Add(texts.ToArray());
            return Task.FromResult<IReadOnlyDictionary<string, ReadOnlyMemory<float>>>(
                texts.ToDictionary(
                    text => text,
                    text => vectorFactory?.Invoke(text) ?? new float[] { 1f, 0f }));
        }
    }

    private sealed class RecordingVectorStore : IVectorStore
    {
        private readonly object _gate = new();

        public List<VectorIndexDefinition> Indexes { get; } = [];
        public List<IReadOnlyList<VectorRecord>> Upserts { get; } = [];
        public List<(string IndexName, IReadOnlyList<long> Ids)> Deletes { get; } = [];
        public List<VectorSearchRequest> Searches { get; } = [];
        public Func<VectorSearchRequest, IReadOnlyList<VectorSearchResult>>? SearchHandler { get; init; }

        public Task EnsureIndexAsync(VectorIndexDefinition definition, CancellationToken ct)
        {
            lock (_gate) Indexes.Add(definition);
            return Task.CompletedTask;
        }

        public Task UpsertAsync(VectorRecord record, CancellationToken ct) =>
            UpsertManyAsync([record], ct);

        public Task UpsertManyAsync(IReadOnlyCollection<VectorRecord> records, CancellationToken ct)
        {
            lock (_gate) Upserts.Add(records.ToArray());
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            VectorSearchRequest request, CancellationToken ct)
        {
            lock (_gate) Searches.Add(request);
            return Task.FromResult(SearchHandler?.Invoke(request) ??
                (IReadOnlyList<VectorSearchResult>)[]);
        }

        public Task DeleteAsync(
            string indexName, IReadOnlyCollection<long> ids, CancellationToken ct)
        {
            lock (_gate) Deletes.Add((indexName, ids.ToArray()));
            return Task.CompletedTask;
        }
    }
}
