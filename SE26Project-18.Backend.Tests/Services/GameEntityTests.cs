using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Tests.Services;

public class GameEntityTests
{
    [Fact]
    public void UpdateDetails_UpdatesAllFields()
    {
        var game = new Game("Old");
        typeof(Game).GetProperty("Id")!.SetValue(game, 1L);

        game.UpdateDetails("New", "NewEN", "Alias1,Alias2", "Co", "Desc", "cover.jpg", "icon.png");

        Assert.Equal("New", game.Name);
        Assert.Equal("NewEN", game.NameEn);
        Assert.Equal("Alias1,Alias2", game.Aliases);
        Assert.Equal("Co", game.Company);
        Assert.Equal("Desc", game.Description);
        Assert.Equal("cover.jpg", game.Cover);
        Assert.Equal("icon.png", game.Icon);
    }

    [Fact]
    public void UpdateTags_ReplacesTagsCollection()
    {
        var game = new Game("Test");
        typeof(Game).GetProperty("Id")!.SetValue(game, 1L);
        game.Tags = new List<GameTag> { new GameTag("Old") };

        game.UpdateTags(new List<GameTag> { new GameTag("New1"), new GameTag("New2") });

        Assert.Equal(2, game.Tags.Count);
        Assert.Contains(game.Tags, t => t.Name == "New1");
        Assert.Contains(game.Tags, t => t.Name == "New2");
    }

    [Fact]
    public void Constructor_SetsNameAndTimestamps()
    {
        var game = new Game("TestGame");

        Assert.Equal("TestGame", game.Name);
        Assert.NotEqual(default, game.CreatedAt);
        Assert.NotEqual(default, game.UpdatedAt);
        Assert.Empty(game.Tags);
    }
}
