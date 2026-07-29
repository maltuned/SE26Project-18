using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Realtime;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chats/{chatId:long}/ws")]
public sealed class MessageWebSocketController : ControllerBase
{
    private readonly IMessageWebSocketHandler _handler;

    public MessageWebSocketController(IMessageWebSocketHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
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

        await _handler.HandleAsync(
            HttpContext,
            chatId,
            GetCurrentUserId(),
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
