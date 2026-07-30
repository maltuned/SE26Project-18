using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Tests.Services;

public sealed class MessageServiceTests
{
    [Fact]
    public async Task Send_EnforcesRestrictedChatAndOpensAfterReply()
    {
        await using var db = CreateDbContext();
        var (chat, user1, user2) = await CreateChatAsync(db);
        var service = new MessageService(db);

        await service.SendAsync(chat.Id, user1.Id, "First message", CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SendAsync(chat.Id, user1.Id, "Too soon", CancellationToken.None)
        );

        await service.SendAsync(chat.Id, user2.Id, "Reply", CancellationToken.None);

        Assert.Equal(ChatStatus.Free, chat.Status);
        Assert.Equal(1, chat.NewMsgsCntForUser1);
        Assert.Equal(1, chat.NewMsgsCntForUser2);
        Assert.Equal(2, await db.Messages.CountAsync());
    }

    [Fact]
    public async Task GetHistory_ReturnsMessagesAndClearsCurrentUsersUnreadCount()
    {
        await using var db = CreateDbContext();
        var (chat, user1, user2) = await CreateChatAsync(db);
        var service = new MessageService(db);
        await service.SendAsync(chat.Id, user1.Id, "First message", CancellationToken.None);

        var history = await service.GetHistoryAsync(chat.Id, user2.Id, CancellationToken.None);

        var message = Assert.Single(history);
        Assert.Equal(user1.Id, message.SenderId);
        Assert.Equal("First message", message.Content);
        Assert.Equal(0, chat.NewMsgsCntForUser2);
    }

    [Fact]
    public async Task GetHistory_RejectsNonParticipant()
    {
        await using var db = CreateDbContext();
        var (chat, _, _) = await CreateChatAsync(db);
        var outsider = new User("outsider", "hash", UserRole.User);
        db.Users.Add(outsider);
        await db.SaveChangesAsync();
        var service = new MessageService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetHistoryAsync(chat.Id, outsider.Id, CancellationToken.None)
        );
    }

    private static async Task<(Chat Chat, User User1, User User2)> CreateChatAsync(
        AppDbContext db
    )
    {
        var user1 = new User("user1", "hash", UserRole.User);
        var user2 = new User("user2", "hash", UserRole.User);
        var game = new Game("game");
        var recruitment = new Recruitment(
            game,
            user1,
            "recruitment",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var chat = new Chat(recruitment, user1, user2);
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
        return (chat, user1, user2);
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
