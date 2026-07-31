using SE26Project_18.Backend.Data;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SE26Project_18.Backend.Hubs;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class MessageServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();
    private readonly MapperService _mapper = new();

    private Mock<IHubContext<ChatHub>> CreateHubMock()
    {
        var hubMock = new Mock<IHubContext<ChatHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();

        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        return hubMock;
    }

    private (User, User, Chat) SeedChat(AppDbContext db, ChatStatus status = ChatStatus.Restricted)
    {
        var u1 = new User("sender", "pw") { Nickname = "Sender" };
        var u2 = new User("receiver", "pw") { Nickname = "Receiver" };
        db.Users.AddRange(u1, u2);
        db.SaveChanges();
        var chat = new Chat { RecruitmentId = 1, RecruiterId = u1.Id, ResponserId = u2.Id, ChatStatus = status };
        db.Chats.Add(chat);
        db.SaveChanges();
        return (u1, u2, chat);
    }

    [Fact]
    public async Task SendMessage_Throws_WhenSenderNotFound()
    {
        var db = CreateDb();
        var (_, u2, chat) = SeedChat(db);
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SendMessageAsync(chat.Id, 999, u2.Id, "Hello"));
    }

    [Fact]
    public async Task SendMessage_Throws_WhenReceiverNotFound()
    {
        var db = CreateDb();
        var (u1, _, chat) = SeedChat(db);
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SendMessageAsync(chat.Id, u1.Id, 999, "Hello"));
    }

    [Fact]
    public async Task SendMessage_Throws_WhenChatNotFound()
    {
        var db = CreateDb();
        var u1 = new User("user", "pw");
        db.Users.Add(u1);
        await db.SaveChangesAsync();
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SendMessageAsync(999, u1.Id, u1.Id, "Hello"));
    }

    [Fact]
    public async Task SendMessage_Throws_WhenChatClosed()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db, ChatStatus.Closed);
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendMessageAsync(chat.Id, u1.Id, u2.Id, "Hello"));
    }

    [Fact]
    public async Task SendMessage_Succeeds_WhenOpen()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db, ChatStatus.Open);
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        var result = await service.SendMessageAsync(chat.Id, u1.Id, u2.Id, "Hello world");

        Assert.NotNull(result);
        Assert.Equal("Hello world", result.Content);
        Assert.Equal(u1.Id, result.SenderId);
    }

    [Fact]
    public async Task SendMessage_PromotesChatToOpen_WhenRestrictedAndBothSent()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db, ChatStatus.Restricted);
        // receiver has already sent a message
        db.Messages.Add(new Message("From receiver") { ChatId = chat.Id, SenderId = u2.Id, ReceiverId = u1.Id });
        await db.SaveChangesAsync();
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await service.SendMessageAsync(chat.Id, u1.Id, u2.Id, "From sender");

        var updatedChat = db.Chats.First();
        Assert.Equal(ChatStatus.Open, updatedChat.ChatStatus);
    }

    [Fact]
    public async Task SendMessage_Throws_WhenRestrictedAndSenderAlreadySentAndReceiverNot()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db, ChatStatus.Restricted);
        // sender already sent
        db.Messages.Add(new Message("First") { ChatId = chat.Id, SenderId = u1.Id, ReceiverId = u2.Id });
        await db.SaveChangesAsync();
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendMessageAsync(chat.Id, u1.Id, u2.Id, "Second message"));
    }

    [Fact]
    public async Task GetMessagesByChat_ReturnsMessages()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db);
        db.Messages.Add(new Message("Msg1") { ChatId = chat.Id, SenderId = u1.Id, ReceiverId = u2.Id });
        db.Messages.Add(new Message("Msg2") { ChatId = chat.Id, SenderId = u2.Id, ReceiverId = u1.Id });
        await db.SaveChangesAsync();
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        var result = await service.GetMessagesByChatAsync(chat.Id);

        Assert.Equal(2, result.Count);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdateAsync")]
    public async Task MarkAsRead_MarksMessagesRead()
    {
        var db = CreateDb();
        var (u1, u2, chat) = SeedChat(db);
        db.Messages.Add(new Message("Msg") { ChatId = chat.Id, SenderId = u1.Id, ReceiverId = u2.Id, IsRead = false });
        await db.SaveChangesAsync();
        var hubMock = CreateHubMock();
        var service = new MessageService(db, _mapper, hubMock.Object);

        await service.MarkAsReadAsync(chat.Id, u2.Id);

        var msg = db.Messages.First();
        Assert.True(msg.IsRead);
    }
}
