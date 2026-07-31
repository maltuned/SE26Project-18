using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Recommendations;
using SE26Project_18.Backend.Services;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Tests.Recommendations;

public sealed class RecommendationTests
{
    [Fact]
    public void Scorer_PrioritizesRecruitmentTags()
    {
        Assert.Equal(0.65d, RecommendationScorer.Combine(1d, 0d), 8);
        Assert.Equal(0.35d, RecommendationScorer.Combine(0d, 1d), 8);
        Assert.Equal(0.4d, RecommendationScorer.Combine(null, 0.4d), 8);
    }

    [Theory]
    [InlineData(0, 0d)]
    [InlineData(1, 0.5d)]
    [InlineData(2, 1d)]
    [InlineData(3, 1.5d)]
    [InlineData(4, 1.5d)]
    public void ViewWeight_IsCappedAfterThreeViews(int count, double expected)
    {
        Assert.Equal(expected, RecommendationBehaviorWeights.GetViewWeight(count));
    }

    [Fact]
    public async Task UserProfile_ContainsRecruitmentAndGamePreferencesFromViews()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var viewer = new User("viewer", "hash");
        var publisher = new User("publisher", "hash");
        var gameTag = new GameTag("RPG");
        var recruitmentTag = new RecruitmentTag("休闲");
        var game = new Game("game") { Tags = [gameTag] };
        var recruitment = new Recruitment("title", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = publisher,
            Game = game,
            GameTags = [gameTag],
            RecruitmentTags = [recruitmentTag],
        };
        db.AddRange(viewer, publisher, game, recruitment);
        await db.SaveChangesAsync();

        var view = new RecruitmentView(viewer, recruitment);
        view.RecordView();
        view.RecordView();
        db.RecruitmentViews.Add(view);
        await db.SaveChangesAsync();

        var tagBuilder = new TagEmbeddingBuilder(
            new StubEmbeddingService(),
            Options.Create(new EmbeddingOptions { Dimension = 2 }));
        var builder = new EmbeddingProfileBatchBuilder(db, tagBuilder);

        var profile = Assert.Single(await builder.BuildUsersAsync([viewer.Id], default));

        Assert.Equal(new float[] { 1f, 0f }, profile.RecruitmentTagVector!.Value.ToArray());
        Assert.Equal(new float[] { 0f, 1f }, profile.GameTagVector!.Value.ToArray());
    }

    [Fact]
    public async Task RecordView_SchedulesOnlyTheFirstThreeViews()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);
        var viewer = new User("viewer", "hash");
        var publisher = new User("publisher", "hash");
        db.Users.AddRange(viewer, publisher);
        await db.SaveChangesAsync();
        var recruitment = new Recruitment("title", DateTime.UtcNow.AddDays(1), 4)
        {
            Publisher = publisher,
            PublisherId = publisher.Id,
        };
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(
            db,
            new MapperService(),
            new PassthroughAlgorithm(),
            new EmbeddingSyncScheduler(db),
            new HttpContextAccessor(),
            NullLogger<RecruitmentService>.Instance);

        for (var i = 0; i < 4; i++)
            Assert.True(await service.RecordViewAsync(viewer.Id, recruitment.Id));

        var view = Assert.Single(await db.RecruitmentViews.ToListAsync());
        Assert.Equal(4, view.ViewCount);
        Assert.Equal(3, await db.EmbeddingSyncOutbox.CountAsync(message =>
            message.Target == EmbeddingTarget.User && message.EntityId == viewer.Id));
    }

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyCollection<string> texts,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyDictionary<string, ReadOnlyMemory<float>>>(
                texts.ToDictionary(
                    text => text,
                    text => (ReadOnlyMemory<float>)(text.StartsWith("recruitment tag:")
                        ? new float[] { 1f, 0f }
                        : new float[] { 0f, 1f })));
        }
    }

    private sealed class PassthroughAlgorithm : IRecruitmentRecommendationAlgorithm
    {
        public Task<IReadOnlyList<long>> RankAsync(
            long userId,
            IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<long>>(candidates.Select(item => item.Id).ToList());
        }
    }
}
