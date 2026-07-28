using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
internal sealed class MessageController : ControllerBase
{
    private readonly IMessageService _service;

    public MessageController(IMessageService service)
    {
        _service = service;
    }

    [HttpGet("chats/{chatId:long}/messages")]
    public async Task<ActionResult<List<MessageResponse>>> GetMessages(long chatId, CancellationToken ct)
    {
        return Ok(await _service.GetByChatIdAsync(chatId, GetCurrentUserId(), ct));
    }

    [HttpPost("messages")]
    public async Task<ActionResult<MessageResponse>> Send(
        [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        return Ok(await _service.SendAsync(req.ChatId, GetCurrentUserId(), req.Content, ct));
    }

    private long GetCurrentUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new AuthenticationException("Token does not contain a valid user identifier.");
        return userId;
    }
}

public class SendMessageRequest
{
    public long ChatId { get; set; }
    public string Content { get; set; } = string.Empty;
}
