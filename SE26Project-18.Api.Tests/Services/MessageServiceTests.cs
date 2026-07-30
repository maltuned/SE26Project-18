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

        var sentMessage = await service.SendAsync(
            chat.Id,
            user1.Id,
            "First message",
            CancellationToken.None
        );

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SendAsync(chat.Id, user1.Id, "Too soon", CancellationToken.None)
        );

        await service.SendAsync(chat.Id, user2.Id, "Reply", CancellationToken.None);

        Assert.Equal(ChatStatus.Free, chat.Status);
        Assert.True(sentMessage.Id > 0);
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

        var history = await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            null,
            50,
            CancellationToken.None
        );

        var message = Assert.Single(history.Items);
        Assert.Equal(user1.Id, message.SenderId);
        Assert.True(message.Id > 0);
        Assert.Equal("First message", message.Content);
        Assert.Equal(0, chat.NewMsgsCntForUser2);
    }

    [Fact]
    public async Task GetHistory_PagesNewestMessagesWithStableTieBreakAndAscendingItems()
    {
        await using var db = CreateDbContext();
        var (chat, user1, user2) = await CreateChatAsync(db);
        var sentAt = new DateTime(2026, 1, 2, 3, 4, 5);
        for (var index = 1; index <= 5; index++)
        {
            chat.Messages.Add(new Message(user1, $"Message {index}", sentAt));
        }

        chat.RecordUnreadMessage(user1.Id);
        await db.SaveChangesAsync();
        var service = new MessageService(db);

        var firstPage = await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            null,
            2,
            CancellationToken.None
        );
        var secondPage = await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            firstPage.NextCursor,
            2,
            CancellationToken.None
        );
        var thirdPage = await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            secondPage.NextCursor,
            2,
            CancellationToken.None
        );

        Assert.Equal(["Message 4", "Message 5"], firstPage.Items.Select(x => x.Content));
        Assert.Equal(["Message 2", "Message 3"], secondPage.Items.Select(x => x.Content));
        Assert.Equal(["Message 1"], thirdPage.Items.Select(x => x.Content));
        Assert.True(firstPage.HasMore);
        Assert.True(secondPage.HasMore);
        Assert.False(thirdPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);
        Assert.NotNull(secondPage.NextCursor);
        Assert.Null(thirdPage.NextCursor);
        Assert.Equal(
            5,
            firstPage
                .Items.Concat(secondPage.Items)
                .Concat(thirdPage.Items)
                .Select(x => x.Id)
                .Distinct()
                .Count()
        );
        Assert.Equal(0, chat.NewMsgsCntForUser2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetHistory_RejectsLimitOutsideAllowedRange(int limit)
    {
        await using var db = CreateDbContext();
        var (chat, user1, _) = await CreateChatAsync(db);
        var service = new MessageService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetHistoryAsync(chat.Id, user1.Id, null, limit, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetHistory_RejectsMalformedCursorWithoutClearingUnreadCount()
    {
        await using var db = CreateDbContext();
        var (chat, user1, user2) = await CreateChatAsync(db);
        chat.RecordUnreadMessage(user1.Id);
        await db.SaveChangesAsync();
        var service = new MessageService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetHistoryAsync(chat.Id, user2.Id, "not-a-cursor", 50, CancellationToken.None)
        );

        Assert.Equal(1, chat.NewMsgsCntForUser2);
    }

    [Fact]
    public async Task GetHistory_OlderPageDoesNotClearUnreadCount()
    {
        await using var db = CreateDbContext();
        var (chat, user1, user2) = await CreateChatAsync(db);
        for (var index = 1; index <= 3; index++)
        {
            chat.Messages.Add(new Message(user1, $"Message {index}", DateTime.UtcNow.AddMinutes(index)));
        }

        await db.SaveChangesAsync();
        var service = new MessageService(db);
        var newestPage = await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            null,
            1,
            CancellationToken.None
        );
        chat.RecordUnreadMessage(user1.Id);
        await db.SaveChangesAsync();

        await service.GetHistoryAsync(
            chat.Id,
            user2.Id,
            newestPage.NextCursor,
            1,
            CancellationToken.None
        );

        Assert.Equal(1, chat.NewMsgsCntForUser2);
    }

    [Fact]
    public async Task SuspendedUser_CannotMarkReadOrSend()
    {
        await using var db = CreateDbContext();
        var (chat, user1, _) = await CreateChatAsync(db);
        user1.Status = UserStatus.Suspended;
        await db.SaveChangesAsync();
        var service = new MessageService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.MarkAsReadAsync(chat.Id, user1.Id, CancellationToken.None)
        );
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SendAsync(chat.Id, user1.Id, "Blocked", CancellationToken.None)
        );

        Assert.Empty(await db.Messages.ToListAsync());
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
            service.GetHistoryAsync(chat.Id, outsider.Id, null, 50, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetHistory_ChecksAuthorizationBeforeCursorValidation()
    {
        await using var db = CreateDbContext();
        var (chat, _, _) = await CreateChatAsync(db);
        var outsider = new User("outsider", "hash", UserRole.User);
        db.Users.Add(outsider);
        await db.SaveChangesAsync();
        var service = new MessageService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetHistoryAsync(
                chat.Id,
                outsider.Id,
                "not-a-cursor",
                50,
                CancellationToken.None
            )
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
