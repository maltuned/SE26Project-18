using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ChatServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    private (User, User, Game, Recruitment) SeedBasicData(AppDbContext db)
    {
        var u1 = new User("recruiter", "pw") { Nickname = "Recruiter" };
        var u2 = new User("responser", "pw") { Nickname = "Responser" };
        db.Users.AddRange(u1, u2);
        var game = new Game("TestGame");
        db.Games.Add(game);
        db.SaveChanges();
        var recruitment = new Recruitment("Looking for team", DateTime.UtcNow.AddDays(30), 5)
        {
            PublisherId = u1.Id, GameId = game.Id, Publisher = u1, Game = game
        };
        db.Recruitments.Add(recruitment);
        db.SaveChanges();
        return (u1, u2, game, recruitment);
    }

    [Fact]
    public async Task GetChatByUsers_ReturnsNull_WhenLessThanTwoIds()
    {
        var db = CreateDb();
        var service = new ChatService(db, _mapper);

        var result = await service.GetChatByUsersAsync(new long[] { 1 });

        Assert.Null(result);
    }

    [Fact]
    public async Task GetChatByUsers_ReturnsNull_WhenNoChat()
    {
        var db = CreateDb();
        var service = new ChatService(db, _mapper);

        var result = await service.GetChatByUsersAsync(new long[] { 1, 2 });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateChat_Throws_WhenRecruitmentNotFound()
    {
        var db = CreateDb();
        var (u1, u2, _, _) = SeedBasicData(db);
        var service = new ChatService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateChatAsync(999, u1.Id, u2.Id));
    }

    [Fact]
    public async Task CreateChat_ReusesExistingChat_WhenExists()
    {
        var db = CreateDb();
        var (u1, u2, _, recruitment) = SeedBasicData(db);
        var existingChat = new Chat { RecruitmentId = 1, RecruiterId = u1.Id, ResponserId = u2.Id };
        db.Chats.Add(existingChat);
        await db.SaveChangesAsync();
        var service = new ChatService(db, _mapper);

        var result = await service.CreateChatAsync(recruitment.Id, u1.Id, u2.Id);

        Assert.NotNull(result);
        Assert.Equal(existingChat.Id, result.Id);
        Assert.Equal(recruitment.Id, result.RecruitmentId);
    }

    [Fact]
    public async Task CreateChat_CreatesNew_WhenNotExists()
    {
        var db = CreateDb();
        var (u1, u2, _, recruitment) = SeedBasicData(db);
        var service = new ChatService(db, _mapper);

        var result = await service.CreateChatAsync(recruitment.Id, u1.Id, u2.Id);

        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
    }

    [Fact]
    public async Task CloseChat_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new ChatService(db, _mapper);

        var result = await service.CloseChatAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task CloseChat_SetsStatusClosed()
    {
        var db = CreateDb();
        var (u1, u2, _, recruitment) = SeedBasicData(db);
        var chat = new Chat { RecruitmentId = recruitment.Id, RecruiterId = u1.Id, ResponserId = u2.Id, ChatStatus = ChatStatus.Open };
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
        var service = new ChatService(db, _mapper);

        var result = await service.CloseChatAsync(chat.Id);

        Assert.True(result);
        Assert.Equal(ChatStatus.Closed, db.Chats.First().ChatStatus);
    }

    [Fact]
    public async Task GetChatsByUser_ReturnsEmpty_WhenNoChats()
    {
        var db = CreateDb();
        var service = new ChatService(db, _mapper);

        var result = await service.GetChatsByUserAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChatById_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new ChatService(db, _mapper);

        var result = await service.GetChatByIdAsync(999, 1);

        Assert.Null(result);
    }
}
