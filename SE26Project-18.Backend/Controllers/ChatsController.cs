using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("by-user")]
    public async Task<ActionResult<ApiResponse<List<ChatBriefDto>>>> GetChatsByUser([FromQuery] long userId)
    {
        var result = await _chatService.GetChatsByUserAsync(userId);
        return Ok(ApiResponse<List<ChatBriefDto>>.Success(result));
    }

    [HttpGet("by-id")]
    public async Task<ActionResult<ApiResponse<ChatDto>>> GetChatById([FromQuery] long chatId, [FromQuery] long userId)
    {
        var result = await _chatService.GetChatByIdAsync(chatId, userId);
        if (result == null)
            return Ok(ApiResponse<ChatDto>.Fail("聊天不存在", 404));
        return Ok(ApiResponse<ChatDto>.Success(result));
    }

    [HttpGet("by-users")]
    public async Task<ActionResult<ApiResponse<ChatDto>>> GetChatByUsers([FromQuery] long[] userIds)
    {
        var result = await _chatService.GetChatByUsersAsync(userIds);
        if (result == null)
            return Ok(ApiResponse<ChatDto>.Success(new ChatDto(), "无聊天记录"));
        return Ok(ApiResponse<ChatDto>.Success(result));
    }

    [HttpGet("by-recruitment")]
    public async Task<ActionResult<ApiResponse<List<ChatDto>>>> GetChatsByRecruitment([FromQuery] long recruitmentId)
    {
        var result = await _chatService.GetChatsByRecruitmentAsync(recruitmentId);
        return Ok(ApiResponse<List<ChatDto>>.Success(result));
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<ChatDto>>> CreateChat([FromBody] CreateChatRequest request)
    {
        try
        {
            var result = await _chatService.CreateChatAsync(request.RecruitmentId, request.User1Id, request.User2Id);
            return Ok(ApiResponse<ChatDto>.Success(result, "创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<ChatDto>.Fail(ex.Message, 404));
        }
    }

    [HttpPost("close")]
    public async Task<ActionResult<ApiResponse<bool>>> CloseChat([FromBody] IdRequest request)
    {
        var result = await _chatService.CloseChatAsync(request.Id);
        return Ok(ApiResponse<bool>.Success(result, result ? "已关闭" : "聊天不存在"));
    }
}

public class CreateChatRequest
{
    [JsonPropertyName("recruitment_id")]
    public long RecruitmentId { get; set; }

    [JsonPropertyName("user1_id")]
    public long User1Id { get; set; }

    [JsonPropertyName("user2_id")]
    public long User2Id { get; set; }
}
