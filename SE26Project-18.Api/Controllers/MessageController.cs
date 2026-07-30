using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chats/{chatId:long}/messages")]
public sealed class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResponse<MessageResponse>>> GetHistory(
        long chatId,
        [FromQuery] string? before,
        [FromQuery] int limit = 50,
        CancellationToken ct = default
    )
    {
        return Ok(
            await _messageService.GetHistoryAsync(
                chatId,
                GetCurrentUserId(),
                before,
                limit,
                ct
            )
        );
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
