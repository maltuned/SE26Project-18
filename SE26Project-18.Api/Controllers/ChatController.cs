using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly ChatService chatService;

    public ChatController(ChatService chatService)
    {
        this.chatService = chatService;
    }

    [HttpGet("by-user/{userId:long}")]
    public async Task<ActionResult<IReadOnlyList<ChatResponse>>> GetChats(long userId)
    {
        var chats = await chatService.GetChatsAsync(userId);
        return Ok(chats);
    }

    [HttpGet("by-users")]
    public async Task<ActionResult<ChatResponse>> GetChatByUsers(
        [FromQuery] long user1Id,
        [FromQuery] long user2Id
    )
    {
        if (user1Id == user2Id)
        {
            return BadRequest("User1Id and User2Id must be different.");
        }

        var chat = await chatService.GetChatByUsersAsync(user1Id, user2Id);

        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> CreateChat(CreateChatRequest request)
    {
        if (request.User1Id == request.User2Id)
        {
            return BadRequest("User1Id and User2Id must be different.");
        }

        if (!await chatService.UsersExistAsync(request.User1Id, request.User2Id))
        {
            return NotFound("User not found.");
        }

        if (!await chatService.RecruitmentExistsAsync(request.RecruitmentId))
        {
            return NotFound("Recruitment not found.");
        }

        var chat = await chatService.CreateChatAsync(
            request.User1Id,
            request.User2Id,
            request.RecruitmentId
        );

        return Ok(chat);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ChatResponse>> GetChatById(long id)
    {
        var chat = await chatService.GetChatByIdAsync(id);

        return chat is null ? NotFound() : Ok(chat);
    }
}
