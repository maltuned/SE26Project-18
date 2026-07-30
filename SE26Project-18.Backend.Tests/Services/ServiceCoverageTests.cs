using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Moq;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Hubs;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ServiceCoverageTests
{
    private readonly MapperService _mapper = new();

    [Fact]
    public async Task Recruitment_CreateWithoutGameId_ThrowsArgumentException()
    {
        await using var db = TestDbContextFactory.Create();
        var service = TestServiceFactory.CreateRecruitmentService(db, _mapper);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateRecruitmentAsync(new RecruitmentDto
            {
                Title = "No game",
                ExpiredAt = DateTime.UtcNow.AddDays(1).ToString("O"),
                MaxParticipants = 2,
            }));

        Assert.Contains("游戏ID不能为空", exception.Message);
    }

    [Fact]
    public async Task Recruitment_RecordView_HandlesMissingAndRepeatedViews()
    {
        await using var db = TestDbContextFactory.Create();
        var (publisher, viewer, _, recruitment) = SeedRecruitment(db);
        var service = TestServiceFactory.CreateRecruitmentService(db, _mapper);

        Assert.False(await service.RecordViewAsync(viewer.Id, 999));
        Assert.False(await service.RecordViewAsync(999, recruitment.Id));
        Assert.True(await service.RecordViewAsync(publisher.Id, recruitment.Id));
        Assert.True(await service.RecordViewAsync(viewer.Id, recruitment.Id));
        Assert.True(await service.RecordViewAsync(viewer.Id, recruitment.Id));

        var view = Assert.Single(db.RecruitmentViews);
        Assert.Equal(viewer.Id, view.UserId);
        Assert.Equal(2, view.ViewCount);
        Assert.Equal(1, view.Version);
    }

    [Fact]
    public async Task Game_Delete_PreservesNameOnAffectedRecruitment()
    {
        await using var db = TestDbContextFactory.Create();
        var (_, _, game, recruitment) = SeedRecruitment(db);
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        var deleted = await service.DeleteGameAsync(game.Id);

        Assert.True(deleted);
        Assert.Null(recruitment.GameId);
        Assert.Equal("Coverage Game", recruitment.GameName);
        Assert.Empty(db.Games);
    }

    [Fact]
    public async Task Game_UpdateImage_ThrowsWhenGameDoesNotExist()
    {
        await using var db = TestDbContextFactory.Create();
        var service = TestServiceFactory.CreateGameService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateGameImageAsync(999, "cover.webp", "icon.webp"));
    }

    [Fact]
    public async Task Tag_DeleteRecruitmentTag_RemovesItFromRecruitments()
    {
        await using var db = TestDbContextFactory.Create();
        var (_, _, _, recruitment) = SeedRecruitment(db);
        var tag = new RecruitmentTag("Voice chat");
        recruitment.RecruitmentTags.Add(tag);
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateTagService(db, _mapper);

        var deleted = await service.DeleteRecruitmentTagAsync(tag.Id);

        Assert.True(deleted);
        Assert.Empty(recruitment.RecruitmentTags);
        Assert.Empty(db.RecruitmentTags);
    }

    [Fact]
    public async Task Response_Delete_ReusesExistingChatAndRestrictsIt()
    {
        await using var db = TestDbContextFactory.Create();
        var (publisher, responder, _, recruitment) = SeedRecruitment(db);
        var response = new Response
        {
            RecruitmentId = recruitment.Id,
            ResponserId = responder.Id,
            Recruitment = recruitment,
            Responser = responder,
        };
        var chat = new Chat
        {
            RecruitmentId = recruitment.Id,
            RecruiterId = publisher.Id,
            ResponserId = responder.Id,
            ChatStatus = ChatStatus.Open,
            Recruitment = recruitment,
            Recruiter = publisher,
            Responser = responder,
        };
        db.AddRange(response, chat);
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateResponseService(db, _mapper);

        var deleted = await service.DeleteResponseAsync(response.Id, "Team is full");

        Assert.True(deleted);
        Assert.Single(db.Chats);
        Assert.Equal(ChatStatus.Restricted, chat.ChatStatus);
        Assert.Equal(ResponseStatus.Deleted, response.ResponseStatus);
        var rejection = Assert.Single(db.Messages);
        Assert.Equal(chat.Id, rejection.ChatId);
        Assert.Contains("Team is full", rejection.Content);
    }

    [Fact]
    public async Task Response_UpdateStatus_UpdatesExistingResponse()
    {
        await using var db = TestDbContextFactory.Create();
        var (_, responder, _, recruitment) = SeedRecruitment(db);
        var response = new Response
        {
            RecruitmentId = recruitment.Id,
            ResponserId = responder.Id,
            Recruitment = recruitment,
            Responser = responder,
        };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        var service = TestServiceFactory.CreateResponseService(db, _mapper);

        var result = await service.UpdateResponseStatusAsync(response.Id, "已删除");

        Assert.NotNull(result);
        Assert.Equal("已删除", result.ResponseStatus);
        Assert.Equal(ResponseStatus.Deleted, response.ResponseStatus);
    }

    [Fact]
    public async Task Chat_GetByUser_CountsOnlyUnreadMessagesForThatUser()
    {
        await using var db = TestDbContextFactory.Create();
        var (publisher, responder, _, recruitment) = SeedRecruitment(db);
        var chat = SeedChat(db, publisher, responder, recruitment);
        db.Messages.AddRange(
            NewMessage(chat, responder, publisher, "Unread one"),
            NewMessage(chat, responder, publisher, "Unread two"),
            NewMessage(chat, publisher, responder, "Unread for the other user"),
            NewMessage(chat, responder, publisher, "Already read", isRead: true));
        await db.SaveChangesAsync();
        var service = new ChatService(db, _mapper);

        var result = await service.GetChatsByUserAsync(publisher.Id);

        var dto = Assert.Single(result);
        Assert.Equal(2, dto.UnreadCount);
        Assert.Equal("Already read", dto.LastMessageContent);
        Assert.Equal(responder.Nickname, dto.OtherUserName);
    }

    [Fact]
    public async Task Chat_Create_ThrowsWhenResponserDoesNotExist()
    {
        await using var db = TestDbContextFactory.Create();
        var (publisher, _, _, recruitment) = SeedRecruitment(db);
        var service = new ChatService(db, _mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateChatAsync(recruitment.Id, publisher.Id, 999));
    }

    [Fact]
    public void Image_InitializationCreatesMissingBucketAndBuildsSslUrl()
    {
        var client = new Mock<IMinioClient>();
        client.Setup(item => item.BucketExistsAsync(
                It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        client.Setup(item => item.MakeBucketAsync(
                It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        client.Setup(item => item.SetPolicyAsync(
                It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ImageService(client.Object, CreateMinioConfiguration(useSsl: true));

        Assert.Equal(
            "https://minio.test:9000/coverage-bucket/avatars/user.webp",
            service.GetPublicUrl("avatars/user.webp"));
        client.Verify(item => item.MakeBucketAsync(
            It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(item => item.SetPolicyAsync(
            It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notification_MarkAsRead_DoesNotAllowAnotherUserToMarkIt()
    {
        await using var db = TestDbContextFactory.Create();
        var owner = new User("notification-owner", "pw");
        var other = new User("notification-other", "pw");
        db.Users.AddRange(owner, other);
        await db.SaveChangesAsync();
        var notification = new Notification(owner.Id, "Private", "Owner only");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var result = await service.MarkAsReadAsync(notification.Id, other.Id);

        Assert.False(result);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task Message_FirstRestrictedMessage_IsSentAndBroadcastToChatAndReceiver()
    {
        await using var db = TestDbContextFactory.Create();
        var (publisher, responder, _, recruitment) = SeedRecruitment(db);
        var chat = SeedChat(db, publisher, responder, recruitment);
        await db.SaveChangesAsync();
        var (hub, clients, proxy) = CreateHubMock();
        var service = new MessageService(db, _mapper, hub.Object);

        var result = await service.SendMessageAsync(
            chat.Id, publisher.Id, responder.Id, "First contact");

        Assert.Equal("First contact", result.Content);
        Assert.Equal(ChatStatus.Restricted, chat.ChatStatus);
        Assert.NotNull(chat.NewMessageAt);
        Assert.Single(db.Messages);
        clients.Verify(item => item.Group($"chat_{chat.Id}"), Times.Once);
        clients.Verify(item => item.Group($"user_{responder.Id}"), Times.Once);
        proxy.Verify(item => item.SendCoreAsync(
            "ReceiveMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        proxy.Verify(item => item.SendCoreAsync(
            "NewChatMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (User Publisher, User Responder, Game Game, Recruitment Recruitment)
        SeedRecruitment(AppDbContext db)
    {
        var publisher = new User("coverage-publisher", "pw") { Nickname = "Publisher" };
        var responder = new User("coverage-responder", "pw") { Nickname = "Responder" };
        var game = new Game("Coverage Game");
        db.AddRange(publisher, responder, game);
        db.SaveChanges();

        var recruitment = new Recruitment("Coverage recruitment", DateTime.UtcNow.AddDays(7), 4)
        {
            PublisherId = publisher.Id,
            Publisher = publisher,
            GameId = game.Id,
            Game = game,
        };
        db.Recruitments.Add(recruitment);
        db.SaveChanges();
        return (publisher, responder, game, recruitment);
    }

    private static Chat SeedChat(
        AppDbContext db,
        User publisher,
        User responder,
        Recruitment recruitment)
    {
        var chat = new Chat
        {
            RecruitmentId = recruitment.Id,
            RecruiterId = publisher.Id,
            ResponserId = responder.Id,
            ChatStatus = ChatStatus.Restricted,
            Recruitment = recruitment,
            Recruiter = publisher,
            Responser = responder,
        };
        db.Chats.Add(chat);
        db.SaveChanges();
        return chat;
    }

    private static Message NewMessage(
        Chat chat,
        User sender,
        User receiver,
        string content,
        bool isRead = false) =>
        new(content)
        {
            ChatId = chat.Id,
            Chat = chat,
            SenderId = sender.Id,
            Sender = sender,
            ReceiverId = receiver.Id,
            Receiver = receiver,
            IsRead = isRead,
        };

    private static IConfiguration CreateMinioConfiguration(bool useSsl)
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(item => item["Endpoint"]).Returns("minio.test:9000");
        section.Setup(item => item["BucketName"]).Returns("coverage-bucket");
        section.Setup(item => item["UseSsl"]).Returns(useSsl.ToString());
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(item => item.GetSection("Minio")).Returns(section.Object);
        return configuration.Object;
    }

    private static (Mock<IHubContext<ChatHub>> Hub, Mock<IHubClients> Clients, Mock<IClientProxy> Proxy)
        CreateHubMock()
    {
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(item => item.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(item => item.Group(It.IsAny<string>())).Returns(proxy.Object);
        var hub = new Mock<IHubContext<ChatHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return (hub, clients, proxy);
    }
}
