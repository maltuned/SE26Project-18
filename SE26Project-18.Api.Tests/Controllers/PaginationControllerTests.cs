using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Controllers;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Tests.Controllers;

public sealed class PaginationControllerTests
{
    [Fact]
    public async Task ChatController_UsesDefaultPageLimit()
    {
        var service = new RecordingChatService();
        var controller = WithUser(new ChatController(service), 17);

        await controller.GetChats(null);

        Assert.Equal(17, service.UserId);
        Assert.Equal(20, service.Limit);
    }

    [Fact]
    public async Task MessageController_UsesDefaultPageLimit()
    {
        var service = new RecordingMessageService();
        var controller = WithUser(new MessageController(service), 17);

        await controller.GetHistory(23, null);

        Assert.Equal(23, service.ChatId);
        Assert.Equal(17, service.UserId);
        Assert.Equal(50, service.Limit);
    }

    private static T WithUser<T>(T controller, long userId)
        where T : ControllerBase
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test"
        );
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
        return controller;
    }

    private sealed class RecordingChatService : IChatService
    {
        public long UserId { get; private set; }

        public int Limit { get; private set; }

        public Task<CursorPagedResponse<ChatResponse>> GetChatsAsync(
            long userId,
            string? before,
            int limit,
            CancellationToken ct
        )
        {
            UserId = userId;
            Limit = limit;
            return Task.FromResult(new CursorPagedResponse<ChatResponse>([], null, false));
        }

        public Task<ChatResponse?> GetChatByUserAsync(
            long currentUserId,
            long otherUserId,
            CancellationToken ct
        ) => throw new NotSupportedException();

        public Task<ChatResponse?> GetChatByIdAsync(
            long id,
            long currentUserId,
            CancellationToken ct
        ) => throw new NotSupportedException();
    }

    private sealed class RecordingMessageService : IMessageService
    {
        public long ChatId { get; private set; }

        public long UserId { get; private set; }

        public int Limit { get; private set; }

        public Task<CursorPagedResponse<MessageResponse>> GetHistoryAsync(
            long chatId,
            long userId,
            string? before,
            int limit,
            CancellationToken ct
        )
        {
            ChatId = chatId;
            UserId = userId;
            Limit = limit;
            return Task.FromResult(new CursorPagedResponse<MessageResponse>([], null, false));
        }

        public Task MarkAsReadAsync(long chatId, long userId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<MessageResponse> SendAsync(
            long chatId,
            long senderId,
            string content,
            CancellationToken ct
        ) => throw new NotSupportedException();
    }
}
