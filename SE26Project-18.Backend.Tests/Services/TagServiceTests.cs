using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class TagServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    [Fact]
    public async Task GetGameTags_ReturnsAll()
    {
        var db = CreateDb();
        db.GameTags.Add(new GameTag("RPG"));
        db.GameTags.Add(new GameTag("FPS"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.GetGameTagsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecruitmentTags_ReturnsAll()
    {
        var db = CreateDb();
        db.RecruitmentTags.Add(new RecruitmentTag("Casual"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.GetRecruitmentTagsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateGameTag_CreatesAndReturns()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.CreateGameTagAsync("NewTag");

        Assert.NotNull(result);
        Assert.Equal("NewTag", result.Name);
        Assert.Single(db.GameTags);
    }

    [Fact]
    public async Task CreateRecruitmentTag_CreatesAndReturns()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.CreateRecruitmentTagAsync("RecTag");

        Assert.Equal("RecTag", result.Name);
    }

    [Fact]
    public async Task UpdateGameTag_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.UpdateGameTagAsync(999, "New");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateGameTag_UpdatesName()
    {
        var db = CreateDb();
        db.GameTags.Add(new GameTag("Old"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.UpdateGameTagAsync(db.GameTags.First().Id, "NewName");

        Assert.NotNull(result);
        Assert.Equal("NewName", result.Name);
    }

    [Fact]
    public async Task DeleteGameTag_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.DeleteGameTagAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteGameTag_RemovesTag()
    {
        var db = CreateDb();
        db.GameTags.Add(new GameTag("ToDelete"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.DeleteGameTagAsync(db.GameTags.First().Id);

        Assert.True(result);
        Assert.Empty(db.GameTags);
    }

    [Fact]
    public async Task DeleteRecruitmentTag_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.DeleteRecruitmentTagAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateRecruitmentTag_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new TagService(db, _mapper);

        var result = await service.UpdateRecruitmentTagAsync(999, "New");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRecruitmentTag_UpdatesName()
    {
        var db = CreateDb();
        db.RecruitmentTags.Add(new RecruitmentTag("Old"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.UpdateRecruitmentTagAsync(db.RecruitmentTags.First().Id, "New");

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
    }

    [Fact]
    public async Task DeleteRecruitmentTag_RemovesTag()
    {
        var db = CreateDb();
        db.RecruitmentTags.Add(new RecruitmentTag("ToDelete"));
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.DeleteRecruitmentTagAsync(db.RecruitmentTags.First().Id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteGameTag_RemovesFromGames()
    {
        var db = CreateDb();
        var tag = new GameTag("RPG");
        db.GameTags.Add(tag);
        var game = new Game("Test") { Tags = new List<GameTag> { tag } };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var service = new TagService(db, _mapper);

        var result = await service.DeleteGameTagAsync(tag.Id);

        Assert.True(result);
        Assert.Empty(db.GameTags);
    }
}
