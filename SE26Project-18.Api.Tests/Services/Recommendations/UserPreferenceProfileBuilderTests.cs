using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services.Recommendations;

public sealed class UserPreferenceProfileBuilderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BuildAsync_UsesOnlyAcceptedApplicantTags(bool accepted)
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var responder = new User("responder", "hash", UserRole.User)
        {
            Tags = [new UserTag("friendly")],
        };
        var recruitment = new Recruitment(
            new Game("game"),
            recruiter,
            "title",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var response = new Response(recruitment, responder);
        if (accepted)
            response.Accept();
        else
            response.Reject();
        recruitment.Responses.Add(response);
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        Assert.NotEqual(recruiter.Id, responder.Id);
        Assert.Equal(
            accepted ? 1 : 0,
            await db.Responses.CountAsync(item =>
                item.Recruitment.Recruiter.Id == recruiter.Id
                && item.Type == ResponseType.Accepted
            )
        );
        Assert.Equal(
            0,
            await db.Responses.CountAsync(item => item.Responder.Id == recruiter.Id)
        );
        Assert.Equal(0, await db.RecruitmentViews.CountAsync(item => item.User.Id == recruiter.Id));
        var tagBuilder = new TagEmbeddingBuilder(
            new StubEmbeddingService(),
            Options.Create(new EmbeddingOptions { Dimension = 2 })
        );
        Assert.False(
            (await tagBuilder.BuildAsync([], "user tag", CancellationToken.None)).HasValue
        );
        var batchBuilder = new EmbeddingProfileBatchBuilder(db, tagBuilder);
        var builder = new UserPreferenceProfileBuilder(db, batchBuilder);

        var profile = await builder.BuildAsync(recruiter.Id, CancellationToken.None);

        Assert.Equal(accepted, profile.InterestedUserTagVector.HasValue);
    }

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> EmbedAsync(
            IReadOnlyCollection<string> texts,
            CancellationToken ct
        )
        {
            return Task.FromResult<IReadOnlyDictionary<string, ReadOnlyMemory<float>>>(
                texts.ToDictionary(
                    text => text,
                    _ => (ReadOnlyMemory<float>)new float[] { 1f, 0f }
                )
            );
        }
    }
}
