using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/messages")]
public sealed class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(
        [FromBody] SendMessageRequest request,
        CancellationToken ct
    )
    {
        var message = await _messageService.SendAsync(
            request.ChatId,
            GetCurrentUserId(),
            request.ReceiverId,
            request.Content,
            ct
        );
        return CreatedAtAction(null, message);
    }

    [HttpGet("by-chat")]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetByChat(
        [FromQuery] long chatId,
        CancellationToken ct
    )
    {
        return Ok(await _messageService.GetByChatAsync(chatId, GetCurrentUserId(), ct));
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new AuthenticationException("Token does not contain a valid user identifier.");

        return userId;
    }
}
