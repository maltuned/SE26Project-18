using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class GameServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    [Fact]
    public async Task GetGames_ReturnsAll_WhenEmptyQuery()
    {
        var db = CreateDb();
        db.Games.Add(new Game("Game1"));
        db.Games.Add(new Game("Game2"));
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGamesAsync("");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetGames_SearchesById_WhenNumericQuery()
    {
        var db = CreateDb();
        var g1 = new Game("Game1");
        var g2 = new Game("Game2");
        db.Games.AddRange(g1, g2);
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGamesAsync(g1.Id.ToString());

        Assert.Single(result);
        Assert.Equal(g1.Id, result[0].Id);
    }

    [Fact]
    public async Task GetGames_SearchesByName()
    {
        var db = CreateDb();
        db.Games.Add(new Game("EldenRing") { NameEn = "Elden Ring EN" });
        db.Games.Add(new Game("Minecraft"));
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGamesAsync("Elden");

        Assert.Single(result);
        Assert.Equal("EldenRing", result[0].Name);
    }

    [Fact]
    public async Task GetGames_UsesContains_Search()
    {
        var db = CreateDb();
        var g = new Game("HeroGame") { Description = "A hero game" };
        db.Games.Add(g);
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGamesAsync("Hero");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetGameById_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGameByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateGame_CreatesWithTags()
    {
        var db = CreateDb();
        db.GameTags.Add(new GameTag("RPG"));
        db.GameTags.Add(new GameTag("FPS"));
        await db.SaveChangesAsync();
        var tags = db.GameTags.ToList();
        var service = TestServiceFactory.CreateGameService(db, _mapper);
        var request = new GameRequestDto
        {
            Name = "NewGame", NameEn = "NG", Company = "TestCo",
            Description = "Desc", Cover = "c.jpg", Icon = "i.png",
            TagsId = tags.Select(t => t.Id).ToArray()
        };

        var result = await service.CreateGameAsync(request);

        Assert.NotNull(result);
        Assert.Equal("NewGame", result.Name);
    }

    [Fact]
    public async Task UpdateGame_Throws_WhenNotFound()
    {
        var db = CreateDb();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateGameAsync(999, new GameRequestDto { Name = "X" }));
    }

    [Fact]
    public async Task UpdateGame_UpdatesSuccessfully()
    {
        var db = CreateDb();
        db.Games.Add(new Game("OldName"));
        db.GameTags.Add(new GameTag("RPG"));
        await db.SaveChangesAsync();
        var game = db.Games.First();
        var tag = db.GameTags.First();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.UpdateGameAsync(game.Id, new GameRequestDto
        {
            Name = "NewName", NameEn = "NN", Company = "C", TagsId = new[] { tag.Id }
        });

        Assert.NotNull(result);
        Assert.Equal("NewName", result.Name);
    }

    [Fact]
    public async Task GetGames_SearchesByNameEn()
    {
        var db = CreateDb();
        db.Games.Add(new Game("Chinese") { NameEn = "English" });
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.GetGamesAsync("English");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetGameById_ReturnsGame_WhenFound()
    {
        var db = CreateDb();
        db.Games.Add(new Game("Found"));
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);
        var game = db.Games.First();

        var result = await service.GetGameByIdAsync(game.Id);

        Assert.NotNull(result);
        Assert.Equal("Found", result.Name);
    }

    [Fact]
    public async Task DeleteGame_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.DeleteGameAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteGame_ReturnsTrue_WhenDeleted()
    {
        var db = CreateDb();
        db.Games.Add(new Game("ToDelete"));
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var result = await service.DeleteGameAsync(db.Games.First().Id);

        Assert.True(result);
        Assert.Empty(db.Games);
    }
}
