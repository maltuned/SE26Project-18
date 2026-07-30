using SE26Project_18.Backend.Data;
using System.Text.Json;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class RecruitmentServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    private (User, Game) SeedData(AppDbContext db)
    {
        var user = new User("pub", "pw") { Nickname = "Publisher" };
        db.Users.Add(user);
        var game = new Game("TestGame");
        db.Games.Add(game);
        db.GameTags.Add(new GameTag("RPG"));
        db.RecruitmentTags.Add(new RecruitmentTag("Casual"));
        db.SaveChanges();
        return (user, game);
    }

    [Fact]
    public async Task CreateRecruitment_Throws_WhenGameNotFound()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateRecruitmentAsync(new RecruitmentDto
            {
                PublisherId = user.Id, GameId = 999, Title = "Test",
                Description = "Desc", Status = "open", MaxParticipants = 3, CurrentParticipants = 0,
                ExpiredAt = DateTime.UtcNow.AddDays(30).ToString("o")
            }));
    }

    [Fact]
    public async Task CreateRecruitment_Throws_WhenPublisherNotFound()
    {
        var db = CreateDb();
        var (_, game) = SeedData(db);
        var service = new RecruitmentService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateRecruitmentAsync(new RecruitmentDto
            {
                PublisherId = 999, GameId = game.Id, Title = "Test",
                Description = "Desc", Status = "open", MaxParticipants = 3, CurrentParticipants = 0,
                ExpiredAt = DateTime.UtcNow.AddDays(30).ToString("o")
            }));
    }

    [Fact]
    public async Task CreateRecruitment_Succeeds_WithValidData()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var service = new RecruitmentService(db, _mapper);

        var result = await service.CreateRecruitmentAsync(new RecruitmentDto
        {
            PublisherId = user.Id, GameId = game.Id, Title = "Looking for team",
            Description = "Join us", Status = "open", MaxParticipants = 5, CurrentParticipants = 0,
            ExpiredAt = DateTime.UtcNow.AddDays(30).ToString("o")
        });

        Assert.NotNull(result);
        Assert.Equal("Looking for team", result.Title);
    }

    [Fact]
    public async Task GetRecruitments_FiltersByGameName()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var game2 = new Game("DotaGame");
        db.Games.Add(game2);
        await db.SaveChangesAsync();
        var r1 = new Recruitment("Looking for LoL", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        var r2 = new Recruitment("Looking for Dota", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game2.Id, Game = game2 };
        db.Recruitments.AddRange(r1, r2);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        // Filter by game name "TestGame" (name of the first game from SeedData)
        var result = await service.GetRecruitmentsAsync("TestGame");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecruitments_ReturnsAll_WhenNoFilters()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        db.Recruitments.Add(new Recruitment("R1", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        db.Recruitments.Add(new Recruitment("R2", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentsAsync(null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateRecruitment_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(999, new Dictionary<string, object>());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRecruitment_SkipsDeleted()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Deleted Rec", DateTime.UtcNow.AddDays(30), 3)
        {
            PublisherId = user.Id, GameId = game.Id, Status = RecruitmentStatus.Deleted, Game = game
        };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "title", "Changed" } });

        Assert.NotNull(result);
        Assert.Equal("Deleted Rec", result.Title); // Not changed
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesTitle()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Old Title", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "title", "New Title" } });

        Assert.Equal("New Title", result.Title);
    }

    [Fact]
    public async Task DeleteRecruitment_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.DeleteRecruitmentAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRecruitment_SoftDeletes()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("ToDelete", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.DeleteRecruitmentAsync(r.Id);

        Assert.True(result);
        Assert.Equal(RecruitmentStatus.Deleted, db.Recruitments.First().Status);
    }

    [Fact]
    public async Task SearchRecruitments_ById_ForNumericQuery()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r1 = new Recruitment("R1", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        var r2 = new Recruitment("R2", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.AddRange(r1, r2);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.SearchRecruitmentsAsync(r1.Id.ToString());

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecruitmentById_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecruitmentById_ReturnsRecruitment_WhenFound()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Found Rec", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentByIdAsync(r.Id);

        Assert.NotNull(result);
        Assert.Equal("Found Rec", result.Title);
    }

    [Fact]
    public async Task GetRecruitmentsByGame_ReturnsFiltered()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        db.Recruitments.Add(new Recruitment("R1", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        db.Recruitments.Add(new Recruitment("R2", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game, Status = RecruitmentStatus.Closed });
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentsByGameAsync(game.Id);

        Assert.Single(result); // only open ones
    }

    [Fact]
    public async Task GetRecruitmentByChatId_ReturnsRecruitment()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Chat Rec", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var chat = new Chat { RecruitmentId = r.Id, RecruiterId = user.Id, ResponserId = 2 };
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentByChatIdAsync(chat.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesDescription()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Old", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "description", "New desc" } });

        Assert.Equal("New desc", result.Description);
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesStatus()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Old", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "status", "已关闭" } });

        Assert.Equal("已关闭", result.Status);
    }

    [Fact]
    public async Task SearchRecruitments_ByText()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        db.Recruitments.Add(new Recruitment("Alpha squad", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        db.Recruitments.Add(new Recruitment("Beta team", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.SearchRecruitmentsAsync("Alpha");

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesMaxParticipants()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Old", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "max_participants", 10 } });

        Assert.Equal(10, result.MaxParticipants);
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesCurrentParticipants()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var r = new Recruitment("Old", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "current_participants", 2 } });

        Assert.Equal(2, result.CurrentParticipants);
    }

    [Fact]
    public async Task SearchRecruitments_ReturnsAll_WhenEmptyQuery()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        db.Recruitments.Add(new Recruitment("R1", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.SearchRecruitmentsAsync("");

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task UpdateRecruitment_UpdatesTagsId()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        db.RecruitmentTags.Add(new RecruitmentTag("Tag1"));
        await db.SaveChangesAsync();
        var r = new Recruitment("Old", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game };
        db.Recruitments.Add(r);
        await db.SaveChangesAsync();
        var tagIds = db.RecruitmentTags.Select(t => t.Id).ToArray();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.UpdateRecruitmentAsync(r.Id,
            new Dictionary<string, object> { { "tags_id", tagIds } });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecruitmentsByPublisherId_ReturnsFiltered()
    {
        var db = CreateDb();
        var (user, game) = SeedData(db);
        var otherUser = new User("other", "pw");
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();
        db.Recruitments.Add(new Recruitment("R1", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = user.Id, GameId = game.Id, Game = game });
        db.Recruitments.Add(new Recruitment("R2", DateTime.UtcNow.AddDays(30), 3)
            { PublisherId = otherUser.Id, GameId = game.Id, Game = game });
        await db.SaveChangesAsync();
        var service = new RecruitmentService(db, _mapper);

        var result = await service.GetRecruitmentsByPublisherIdAsync(user.Id);

        Assert.Single(result);
    }
}
