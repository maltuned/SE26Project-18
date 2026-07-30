using Microsoft.Data.Sqlite;
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
    public async Task CreateGame_CreatesGameAndSchedulesEmbedding()
    {
        await using var db = CreateDbContext();
        var tag = new GameTag("RPG");
        db.GameTags.Add(tag);
        await db.SaveChangesAsync();
        var service = new GameService(db, new EmbeddingSyncScheduler(db));

        var response = await service.CreateAsync(
            new CreateGameRequest(" game ", "description", [tag.Id]),
            CancellationToken.None
        );

        Assert.Equal("game", response.Name);
        Assert.Equal("description", response.Description);
        Assert.Equal([tag.Id], response.Tags.Select(item => item.Id));
        var message = Assert.Single(await db.EmbeddingSyncOutbox.ToListAsync());
        Assert.Equal(EmbeddingTarget.Game, message.Target);
        Assert.Equal(response.Id, message.EntityId);
    }

    [Fact]
    public async Task CreateGame_RejectsDuplicateName()
    {
        await using var db = CreateDbContext();
        db.Games.Add(new Game("game"));
        await db.SaveChangesAsync();
        var service = new GameService(db, new EmbeddingSyncScheduler(db));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(
                new CreateGameRequest(" game ", string.Empty),
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task UpdateGame_RejectsMissingTag()
    {
        await using var db = CreateDbContext();
        var game = new Game("game");
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var service = new GameService(db, new EmbeddingSyncScheduler(db));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(
                game.Id,
                new UpdateGameRequest(TagIds: [long.MaxValue]),
                CancellationToken.None
            )
        );
    }

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

    [Fact]
    public async Task GetGameTags_ReturnsSortedNoTrackingTags()
    {
        await using var db = CreateDbContext();
        db.GameTags.AddRange(new GameTag("Strategy"), new GameTag("Action"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new TagCatalogService(db);

        var response = await service.GetGameTagsAsync(CancellationToken.None);

        Assert.Equal(["Action", "Strategy"], response.Select(tag => tag.Name));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetRecruitmentTags_ReturnsSortedNoTrackingTags()
    {
        await using var db = CreateDbContext();
        db.RecruitmentTags.AddRange(
            new RecruitmentTag("Veteran"),
            new RecruitmentTag("Beginner")
        );
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new TagCatalogService(db);

        var response = await service.GetRecruitmentTagsAsync(CancellationToken.None);

        Assert.Equal(["Beginner", "Veteran"], response.Select(tag => tag.Name));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetUserTags_ReturnsSortedNoTrackingTags()
    {
        await using var db = CreateDbContext();
        db.UserTags.AddRange(new UserTag("Support"), new UserTag("Competitive"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new TagCatalogService(db);

        var response = await service.GetUserTagsAsync(CancellationToken.None);

        Assert.Equal(["Competitive", "Support"], response.Select(tag => tag.Name));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static AppDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
