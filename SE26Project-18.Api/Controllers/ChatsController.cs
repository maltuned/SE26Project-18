using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Dtos;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Route("api/chats")]
public sealed class ChatsController : ControllerBase
{
    private readonly ChatService chatService;

    public ChatsController(ChatService chatService)
    {
        this.chatService = chatService;
    }

    [HttpGet("/api/users/{userId:long}/chats")]
    public async Task<ActionResult<IReadOnlyList<ChatDto>>> GetChats(long userId)
    {
        var chats = await chatService.GetChatsAsync(userId);
        return Ok(chats);
    }

    [HttpGet("by-users")]
    public async Task<ActionResult<ChatDto>> GetChatByUsers([FromQuery] long[] userIds)
    {
        if (!TryGetUserPair(userIds, out var firstUserId, out var secondUserId))
        {
            return BadRequest("Exactly two different userIds are required.");
        }

        var chat = await chatService.GetChatByUsersAsync(firstUserId, secondUserId);

        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpPost]
    public async Task<ActionResult<ChatDto>> CreateChat(CreateChatRequest request)
    {
        if (!TryGetUserPair(request.UserIds, out var firstUserId, out var secondUserId))
        {
            return BadRequest("Exactly two different userIds are required.");
        }

        if (!await chatService.UsersExistAsync(firstUserId, secondUserId))
        {
            return NotFound("User not found.");
        }

        var recruitmentId = request.RecruitmentId > 0 ? request.RecruitmentId : (long?)null;
        if (recruitmentId is not null && !await chatService.RecruitmentExistsAsync(recruitmentId.Value))
        {
            return NotFound("Recruitment not found.");
        }

        var chat = await chatService.CreateChatAsync(firstUserId, secondUserId, recruitmentId);

        return Ok(chat);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ChatDto>> GetChatById(long id)
    {
        var chat = await chatService.GetChatByIdAsync(id);

        return chat is null ? NotFound() : Ok(chat);
    }

    private static bool TryGetUserPair(long[]? userIds, out long firstUserId, out long secondUserId)
    {
        firstUserId = 0;
        secondUserId = 0;

        if (userIds is not { Length: 2 } || userIds[0] == userIds[1])
        {
            return false;
        }

        firstUserId = userIds[0];
        secondUserId = userIds[1];
        return true;
    }
}
