using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public ChatService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    private IQueryable<Chat> Query()
    {
        return _db.Chats
            .Include(c => c.Recruiter)
            .Include(c => c.Responser)
            .Include(c => c.Recruitment)
            .Include(c => c.Messages).ThenInclude(m => m.Sender)
            .Include(c => c.Messages).ThenInclude(m => m.Receiver);
    }

    public async Task<List<ChatBriefDto>> GetChatsByUserAsync(long userId)
    {
        var chats = await Query()
            .Where(c => c.RecruiterId == userId || c.ResponserId == userId)
            .OrderByDescending(c => c.NewMessageAt ?? c.CreatedAt)
            .ToListAsync();
        return chats.Select(c => _mapper.ToChatBriefDto(c, userId)).ToList();
    }

    public async Task<ChatDto?> GetChatByIdAsync(long chatId, long currentUserId)
    {
        var chat = await Query().FirstOrDefaultAsync(c => c.Id == chatId);
        return chat == null ? null : _mapper.ToChatDto(chat, currentUserId);
    }

    public async Task<List<ChatDto>> GetChatsByRecruitmentAsync(long recruitmentId)
    {
        var chats = await Query()
            .Where(c => c.RecruitmentId == recruitmentId)
            .ToListAsync();
        return chats.Select(c => _mapper.ToChatDto(c, c.RecruiterId)).ToList();
    }

    public async Task<ChatDto?> GetChatByUsersAsync(long[] userIds)
    {
        if (userIds.Length < 2) return null;
        long u1 = userIds[0], u2 = userIds[1];
        var chat = await Query().FirstOrDefaultAsync(c =>
            (c.RecruiterId == u1 && c.ResponserId == u2) ||
            (c.RecruiterId == u2 && c.ResponserId == u1));
        return chat == null ? null : _mapper.ToChatDto(chat, u1);
    }

    public async Task<ChatDto> CreateChatAsync(long recruitmentId, long user1Id, long user2Id)
    {
        // Check if chat already exists between these two users
        var existing = await Query().FirstOrDefaultAsync(c =>
            (c.RecruiterId == user1Id && c.ResponserId == user2Id) ||
            (c.RecruiterId == user2Id && c.ResponserId == user1Id));

        if (existing != null)
        {
            // Update recruitment_id and return
            existing.RecruitmentId = recruitmentId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return _mapper.ToChatDto(existing, user1Id);
        }

        var recruitment = await _db.Recruitments.FindAsync(recruitmentId)
            ?? throw new KeyNotFoundException("招募不存在");
        var recruiter = await _db.Users.FindAsync(user1Id)
            ?? throw new KeyNotFoundException("用户不存在");
        var responser = await _db.Users.FindAsync(user2Id)
            ?? throw new KeyNotFoundException("用户不存在");

        var chat = new Chat
        {
            RecruitmentId = recruitmentId,
            RecruiterId = user1Id,
            ResponserId = user2Id,
            ChatStatus = Models.Enums.ChatStatus.Restricted,
            Recruitment = recruitment,
            Recruiter = recruiter,
            Responser = responser,
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        return _mapper.ToChatDto(chat, user1Id);
    }

    public async Task<bool> CloseChatAsync(long chatId)
    {
        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return false;
        chat.ChatStatus = Models.Enums.ChatStatus.Closed;
        chat.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
