using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

    // 历史消息
    [HttpGet("chats/{chatId:long}/messages")]
    public async Task<ActionResult<List<MessageResponse>>> GetHistory(long chatId, CancellationToken ct)
    {
        return Ok(await _service.GetHistoryAsync(chatId, GetUserId(), ct));
    }

    // WebSocket 实时连接
    [HttpGet("chats/{chatId:long}/ws")]
    public async Task ConnectWebSocket(long chatId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var userId = GetUserId();
        // 验证是聊天参与者
        var history = await _service.GetHistoryAsync(chatId, userId, HttpContext.RequestAborted);

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _service.AddSocket(chatId, ws);

        var buffer = new byte[4096];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), HttpContext.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var req = JsonSerializer.Deserialize<WsMessage>(json);
                if (req != null && !string.IsNullOrWhiteSpace(req.Content))
                {
                    await _service.SaveAndBroadcastAsync(chatId, userId, req.Content, HttpContext.RequestAborted);
                }
            }
        }
        finally
        {
            _service.RemoveSocket(chatId, ws);
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            ws.Dispose();
        }
    }

    private long GetUserId()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new AuthenticationException("Token does not contain a valid user identifier.");
        return userId;
    }

    private class WsMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
