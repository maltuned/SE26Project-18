using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Realtime;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chats/{chatId:long}")]
public sealed class MessageWebSocketController : ControllerBase
{
    private readonly IMessageWebSocketHandler _handler;

    private readonly IChatService _chatService;

    private readonly IWebSocketTicketStore _ticketStore;

    private readonly IUserService _userService;

    public MessageWebSocketController(
        IMessageWebSocketHandler handler,
        IChatService chatService,
        IWebSocketTicketStore ticketStore,
        IUserService userService
    )
    {
        _handler = handler;
        _chatService = chatService;
        _ticketStore = ticketStore;
        _userService = userService;
    }

    [HttpPost("ws-ticket")]
    public async Task<ActionResult<WebSocketTicketResponse>> IssueTicket(
        long chatId,
        CancellationToken ct
    )
    {
        var userId = GetCurrentUserId();
        await _userService.EnsureActiveAsync(userId, ct);
        if (await _chatService.GetChatByIdAsync(chatId, userId, ct) is null)
        {
            throw new NotFoundException("Chat not found.");
        }

        var ticket = _ticketStore.Issue(userId, chatId);
        return Ok(new WebSocketTicketResponse(ticket.Value, ticket.ExpiresAt));
    }

    [AllowAnonymous]
    [HttpGet("ws")]
    public async Task<IActionResult> Connect(long chatId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "WebSocket upgrade required",
                detail: "This endpoint requires a WebSocket upgrade request."
            );
        }

        if (
            !_ticketStore.TryConsume(
                HttpContext.Request.Query["ticket"].ToString(),
                chatId,
                out var userId
            )
        )
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid WebSocket ticket",
                detail: "A valid WebSocket ticket is required."
            );
        }

        await _handler.HandleAsync(
            HttpContext,
            chatId,
            userId,
            HttpContext.RequestAborted
        );

        return new EmptyResult();
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new AuthenticationException("Token does not contain a valid user identifier.");
        }

        return userId;
    }
}
