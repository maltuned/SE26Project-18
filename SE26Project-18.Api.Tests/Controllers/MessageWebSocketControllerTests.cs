using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Controllers;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Realtime;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Tests.Controllers;

public sealed class MessageWebSocketControllerTests
{
    [Fact]
    public async Task IssueTicket_IssuesUserAndChatBoundTicketForParticipant()
    {
        var chatService = new StubChatService
        {
            Chat = new ChatResponse(8, 1, 4, 7, ChatStatus.Free, 0, 0, null),
        };
        var ticketStore = new RecordingTicketStore();
        var controller = CreateController(chatService, ticketStore, new StubUserService(), userId: 7);

        var result = await controller.IssueTicket(8, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var ticket = Assert.IsType<WebSocketTicketResponse>(response.Value);
        Assert.Equal("ticket", ticket.Ticket);
        Assert.Equal(7, ticketStore.UserId);
        Assert.Equal(8, ticketStore.ChatId);
    }

    [Fact]
    public async Task IssueTicket_RejectsNonParticipant()
    {
        var controller = CreateController(
            new StubChatService(),
            new RecordingTicketStore(),
            new StubUserService(),
            userId: 7
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.IssueTicket(8, CancellationToken.None)
        );
    }

    [Fact]
    public async Task IssueTicket_RejectsSuspendedUserUsingCurrentStatus()
    {
        var controller = CreateController(
            new StubChatService
            {
                Chat = new ChatResponse(8, 1, 4, 7, ChatStatus.Free, 0, 0, null),
            },
            new RecordingTicketStore(),
            new StubUserService { Suspended = true },
            userId: 7
        );

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            controller.IssueTicket(8, CancellationToken.None)
        );
    }

    private static MessageWebSocketController CreateController(
        IChatService chatService,
        IWebSocketTicketStore ticketStore,
        IUserService userService,
        long userId
    )
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test"
        );
        return new MessageWebSocketController(
            new StubWebSocketHandler(),
            chatService,
            ticketStore,
            userService
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
    }

    private sealed class StubChatService : IChatService
    {
        public ChatResponse? Chat { get; init; }

        public Task<CursorPagedResponse<ChatResponse>> GetChatsAsync(
            long userId,
            string? before,
            int limit,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<ChatResponse?> GetChatByUserAsync(
            long currentUserId,
            long otherUserId,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<ChatResponse?> GetChatByIdAsync(
            long id,
            long currentUserId,
            CancellationToken ct
        )
        {
            return Task.FromResult(Chat);
        }
    }

    private sealed class RecordingTicketStore : IWebSocketTicketStore
    {
        public long UserId { get; private set; }

        public long ChatId { get; private set; }

        public WebSocketTicket Issue(long userId, long chatId)
        {
            UserId = userId;
            ChatId = chatId;
            return new WebSocketTicket("ticket", DateTimeOffset.UnixEpoch);
        }

        public bool TryConsume(string ticket, long chatId, out long userId)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubWebSocketHandler : IMessageWebSocketHandler
    {
        public Task HandleAsync(
            HttpContext context,
            long chatId,
            long userId,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubUserService : IUserService
    {
        public bool Suspended { get; init; }

        public Task EnsureActiveAsync(long id, CancellationToken ct)
        {
            return Suspended
                ? Task.FromException(new AuthenticationException("User is suspended."))
                : Task.CompletedTask;
        }

        public Task<UserResponse?> GetByIdAsync(long id, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<UserResponse> UpdateAsync(
            long id,
            UpdateUserRequest request,
            CancellationToken ct
        ) => throw new NotSupportedException();

        public Task<UserResponse> SetSuspensionAsync(
            long actorId,
            long id,
            SetUserSuspensionRequest request,
            CancellationToken ct
        ) => throw new NotSupportedException();
    }
}
