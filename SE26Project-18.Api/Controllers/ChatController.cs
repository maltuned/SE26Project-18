using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chats")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CursorPagedResponse<ChatResponse>>> GetChats(
        [FromQuery] string? before,
        [FromQuery] int limit = 20,
        CancellationToken ct = default
    )
    {
        return Ok(await _chatService.GetChatsAsync(GetCurrentUserId(), before, limit, ct));
    }

    [HttpGet("by-user/{userId:long}")]
    public async Task<ActionResult<ChatResponse>> GetChatByUser(long userId, CancellationToken ct)
    {
        var currentUserId = GetCurrentUserId();

        if (userId == currentUserId)
        {
            throw new ValidationException("UserId must identify another user.");
        }

        var chat = await _chatService.GetChatByUserAsync(currentUserId, userId, ct);
        return Ok(chat ?? throw new NotFoundException("Chat not found."));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ChatResponse>> GetChatById(long id, CancellationToken ct)
    {
        var chat = await _chatService.GetChatByIdAsync(id, GetCurrentUserId(), ct);
        if (chat is null)
        {
            throw new NotFoundException("Chat not found.");
        }

        return Ok(chat);
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
