using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Tests.Services;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task GetChats_PagesByLastActivityThenChatIdIncludingChatsWithoutMessages()
    {
        await using var db = CreateDbContext();
        var currentUser = new User("current", "hash", UserRole.User);
        var game = new Game("game");
        db.AddRange(currentUser, game);
        var chats = new List<Chat>();
        for (var index = 1; index <= 5; index++)
        {
            var otherUser = new User($"other-{index}", "hash", UserRole.User);
            var recruitment = new Recruitment(
                game,
                currentUser,
                $"Recruitment {index}",
                2,
                DateTime.UtcNow.AddDays(1)
            );
            var chat = new Chat(recruitment, currentUser, otherUser);
            chats.Add(chat);
            db.Chats.Add(chat);
        }

        await db.SaveChangesAsync();
        var tiedActivity = new DateTime(2026, 2, 3, 4, 5, 6);
        chats[0].Messages.Add(new Message(currentUser, "first tied", tiedActivity));
        chats[1].Messages.Add(new Message(currentUser, "second tied", tiedActivity));
        chats[2].Messages.Add(new Message(currentUser, "newest", tiedActivity.AddMinutes(1)));
        await db.SaveChangesAsync();
        var service = new ChatService(db);

        var firstPage = await service.GetChatsAsync(
            currentUser.Id,
            null,
            2,
            CancellationToken.None
        );
        var secondPage = await service.GetChatsAsync(
            currentUser.Id,
            firstPage.NextCursor,
            2,
            CancellationToken.None
        );
        var thirdPage = await service.GetChatsAsync(
            currentUser.Id,
            secondPage.NextCursor,
            2,
            CancellationToken.None
        );

        Assert.Equal([chats[2].Id, chats[1].Id], firstPage.Items.Select(chat => chat.Id));
        Assert.Equal([chats[0].Id, chats[4].Id], secondPage.Items.Select(chat => chat.Id));
        Assert.Equal([chats[3].Id], thirdPage.Items.Select(chat => chat.Id));
        Assert.Equal(chats[1].Messages.Single().Id, firstPage.Items[1].LastMessage?.Id);
        Assert.True(firstPage.HasMore);
        Assert.True(secondPage.HasMore);
        Assert.False(thirdPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);
        Assert.Matches("^[A-Za-z0-9_-]+$", firstPage.NextCursor);
        Assert.NotNull(secondPage.NextCursor);
        Assert.Null(thirdPage.NextCursor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetChats_RejectsLimitOutsideAllowedRange(int limit)
    {
        await using var db = CreateDbContext();
        var service = new ChatService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetChatsAsync(1, null, limit, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetChats_RejectsMalformedCursor()
    {
        await using var db = CreateDbContext();
        var service = new ChatService(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetChatsAsync(1, "not-a-cursor", 20, CancellationToken.None)
        );

        Assert.Equal("The pagination cursor is invalid.", exception.Message);
    }

    [Fact]
    public async Task GetChatByUser_ReturnsLatestMessageUsingIdAsTieBreaker()
    {
        await using var db = CreateDbContext();
        var currentUser = new User("current", "hash", UserRole.User);
        var otherUser = new User("other", "hash", UserRole.User);
        var game = new Game("game");
        var recruitment = new Recruitment(
            game,
            currentUser,
            "Recruitment",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var chat = new Chat(recruitment, currentUser, otherUser);
        db.Add(chat);
        await db.SaveChangesAsync();
        var sentAt = new DateTime(2026, 2, 3, 4, 5, 6);
        var olderMessage = new Message(currentUser, "older", sentAt);
        var latestMessage = new Message(otherUser, "latest", sentAt);
        chat.Messages.Add(olderMessage);
        chat.Messages.Add(latestMessage);
        await db.SaveChangesAsync();
        var service = new ChatService(db);

        var result = await service.GetChatByUserAsync(
            currentUser.Id,
            otherUser.Id,
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Equal(chat.Id, result.Id);
        Assert.Equal(latestMessage.Id, result.LastMessage?.Id);
        Assert.Equal(otherUser.Id, result.LastMessage?.SenderId);
    }

    [Fact]
    public async Task GetChatById_ReturnsChatWithoutMessages()
    {
        await using var db = CreateDbContext();
        var currentUser = new User("current", "hash", UserRole.User);
        var otherUser = new User("other", "hash", UserRole.User);
        var game = new Game("game");
        var recruitment = new Recruitment(
            game,
            currentUser,
            "Recruitment",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var chat = new Chat(recruitment, currentUser, otherUser);
        db.Add(chat);
        await db.SaveChangesAsync();
        var service = new ChatService(db);

        var result = await service.GetChatByIdAsync(
            chat.Id,
            currentUser.Id,
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Null(result.LastMessage);
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
