using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public sealed class ChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        this._db = db;
    }

    public async Task<IReadOnlyList<ChatResponse>> GetChatsAsync(long userId)
    {
        var chats = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages)
                .ThenInclude(message => message.Sender)
            .Where(chat => chat.User1.Id == userId || chat.User2.Id == userId)
            .ToListAsync();

        return chats
            .OrderByDescending(GetLastMessageTime)
            .Select(chat => chat.ToResponse())
            .ToList();
    }

    public async Task<ChatResponse?> GetChatByUsersAsync(long user1Id, long user2Id)
    {
        var chat = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages)
                .ThenInclude(message => message.Sender)
            .FirstOrDefaultAsync(chat =>
                (chat.User1.Id == user1Id && chat.User2.Id == user2Id)
                || (chat.User1.Id == user2Id && chat.User2.Id == user1Id)
            );

        return chat?.ToResponse();
    }

    public async Task<ChatResponse?> GetChatByIdAsync(long id)
    {
        var chat = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages)
                .ThenInclude(message => message.Sender)
            .FirstOrDefaultAsync(chat => chat.Id == id);

        return chat?.ToResponse();
    }

    public async Task<ChatResponse> CreateChatAsync(long recruitmentId, long user1Id, long user2Id)
    {
        var chat = await _db
            .Chats.Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .FirstOrDefaultAsync(chat =>
                (chat.User1.Id == user1Id && chat.User2.Id == user2Id)
                || (chat.User1.Id == user2Id && chat.User2.Id == user1Id)
            );

        if (chat is null)
        {
            var users = await _db
                .Users.Where(user => user.Id == user1Id || user.Id == user2Id)
                .ToListAsync();
            var recruitment =
                await _db.Recruitments.FirstOrDefaultAsync(r => r.Id == recruitmentId)
                ?? throw new KeyNotFoundException("Recruitment not found.");

            chat = new Chat(
                recruitment,
                users.Single(user => user.Id == user1Id),
                users.Single(user => user.Id == user2Id)
            );
            _db.Chats.Add(chat);
        }

        await _db.SaveChangesAsync();
        return chat.ToResponse();
    }

    public async Task<bool> UsersExistAsync(long user1Id, long user2Id)
    {
        var userIds = new[] { user1Id, user2Id };
        var count = await _db.Users.CountAsync(user => userIds.Contains(user.Id));

        return count == 2;
    }

    public async Task<bool> RecruitmentExistsAsync(long recruitmentId)
    {
        return await _db.Recruitments.AnyAsync(recruitment => recruitment.Id == recruitmentId);
    }

    private static DateTime GetLastMessageTime(Chat chat)
    {
        return chat.Messages.MaxBy(message => message.SentAt)?.SentAt ?? DateTime.MinValue;
    }
}
