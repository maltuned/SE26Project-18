using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services;

public sealed class GameAndTagServiceTests
{
    [Fact]
    public async Task UpdateGameTags_SchedulesGameEmbedding()
    {
        await using var db = CreateDbContext();
        var game = new Game("game");
        var tag = new GameTag("RPG");
        db.AddRange(game, tag);
        await db.SaveChangesAsync();
        var service = new GameService(db, new EmbeddingSyncScheduler(db));

        var response = await service.UpdateAsync(
            game.Id,
            new UpdateGameRequest(TagIds: [tag.Id]),
            CancellationToken.None
        );

        Assert.Equal([tag.Id], response.Tags.Select(item => item.Id));
        var message = Assert.Single(await db.EmbeddingSyncOutbox.ToListAsync());
        Assert.Equal(EmbeddingTarget.Game, message.Target);
        Assert.Equal(game.Id, message.EntityId);
    }

    [Fact]
    public async Task CreateTag_RejectsDuplicateName()
    {
        await using var db = CreateDbContext();
        var service = new TagCatalogService(db);
        await service.CreateGameTagAsync(new CreateTagRequest("RPG"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateGameTagAsync(new CreateTagRequest("RPG"), CancellationToken.None)
        );
    }

    private static AppDbContext CreateDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
    }
}
