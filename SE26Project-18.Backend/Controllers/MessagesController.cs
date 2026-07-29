using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("by-chat")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetMessagesByChat([FromQuery] long chatId)
    {
        var result = await _messageService.GetMessagesByChatAsync(chatId);
        return Ok(ApiResponse<List<MessageDto>>.Success(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var result = await _messageService.SendMessageAsync(request.ChatId, request.SenderId, request.ReceiverId, request.Content);
            return Ok(ApiResponse<MessageDto>.Success(result, "发送成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<MessageDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<MessageDto>.Fail(ex.Message, 403));
        }
    }

    [HttpPost("mark-read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead([FromBody] MarkReadRequest request)
    {
        await _messageService.MarkAsReadAsync(request.ChatId, request.UserId);
        return Ok(ApiResponse<bool>.Success(true));
    }
}

public class SendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("sender_id")]
    public long SenderId { get; set; }

    [JsonPropertyName("receiver_id")]
    public long ReceiverId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class MarkReadRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
}