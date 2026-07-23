using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Dtos;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Services;

public sealed class ChatService
{
    private readonly AppDbContext dbContext;

    public ChatService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ChatDto>> GetChatsAsync(long userId)
    {
        var chats = await dbContext.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .Where(chat => chat.RecruiterId == userId || chat.ResponserId == userId)
            .ToListAsync();

        return chats
            .OrderByDescending(GetLastMessageTime)
            .Select(ToDto)
            .ToList();
    }

    public async Task<ChatDto?> GetChatByUsersAsync(long firstUserId, long secondUserId)
    {
        var chat = await dbContext.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .FirstOrDefaultAsync(chat =>
                (chat.RecruiterId == firstUserId && chat.ResponserId == secondUserId)
                || (chat.RecruiterId == secondUserId && chat.ResponserId == firstUserId));

        return chat is null ? null : ToDto(chat);
    }

    public async Task<ChatDto?> GetChatByIdAsync(long id)
    {
        var chat = await dbContext.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .FirstOrDefaultAsync(chat => chat.Id == id);

        return chat is null ? null : ToDto(chat);
    }

    public async Task<ChatDto> CreateChatAsync(long firstUserId, long secondUserId, long? recruitmentId)
    {
        var chat = await dbContext.Chats
            .Include(chat => chat.Messages)
            .FirstOrDefaultAsync(chat =>
                (chat.RecruiterId == firstUserId && chat.ResponserId == secondUserId)
                || (chat.RecruiterId == secondUserId && chat.ResponserId == firstUserId));

        if (chat is null)
        {
            chat = new Chat(firstUserId, secondUserId, recruitmentId);
            dbContext.Chats.Add(chat);
        }
        else
        {
            chat.RefreshRecruitment(recruitmentId);
        }

        await dbContext.SaveChangesAsync();
        return ToDto(chat);
    }

    public async Task<bool> UsersExistAsync(long firstUserId, long secondUserId)
    {
        var userIds = new[] { firstUserId, secondUserId };
        var count = await dbContext.Users.CountAsync(user => userIds.Contains(user.Id));

        return count == 2;
    }

    public async Task<bool> RecruitmentExistsAsync(long recruitmentId)
    {
        return await dbContext.Recruitments.AnyAsync(recruitment => recruitment.Id == recruitmentId);
    }

    private static ChatDto ToDto(Chat chat)
    {
        var lastMessage = chat.Messages
            .OrderByDescending(message => message.SentAt)
            .FirstOrDefault();

        return new ChatDto(
            chat.Id,
            chat.RecruitmentId,
            chat.RecruiterId,
            chat.ResponserId,
            chat.Status,
            chat.NewMsgsCntForRecruiter,
            chat.NewMsgsCntForResponser,
            lastMessage is null
                ? null
                : new LastMessageDto(lastMessage.SenderId, lastMessage.Content, lastMessage.SentAt));
    }

    private static DateTime GetLastMessageTime(Chat chat)
    {
        return chat.Messages
            .OrderByDescending(message => message.SentAt)
            .Select(message => message.SentAt)
            .FirstOrDefault();
    }
}
