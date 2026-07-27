using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class ChatService : IChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        this._db = db;
    }

    public async Task<IReadOnlyList<ChatResponse>> GetChatsAsync(long userId, CancellationToken ct)
    {
        var chats = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
                .ThenInclude(message => message.Sender)
            .Where(chat => chat.User1.Id == userId || chat.User2.Id == userId)
            .ToListAsync(ct);

        return chats
            .OrderByDescending(chat =>
                chat.Messages.MaxBy(message => message.SentAt)?.SentAt ?? DateTime.MinValue
            )
            .Select(chat => chat.ToResponse())
            .ToList();
    }

    public async Task<ChatResponse?> GetChatByUserAsync(
        long currentUserId,
        long otherUserId,
        CancellationToken ct
    )
    {
        var chat = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
                .ThenInclude(message => message.Sender)
            .FirstOrDefaultAsync(
                chat =>
                    (
                        (chat.User1.Id == currentUserId && chat.User2.Id == otherUserId)
                        || (chat.User1.Id == otherUserId && chat.User2.Id == currentUserId)
                    ),
                ct
            );

        return chat?.ToResponse();
    }

    public async Task<ChatResponse?> GetChatByIdAsync(
        long id,
        long currentUserId,
        CancellationToken ct
    )
    {
        var chat = await _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Include(chat => chat.Messages.OrderByDescending(message => message.SentAt).Take(1))
                .ThenInclude(message => message.Sender)
            .FirstOrDefaultAsync(
                chat =>
                    chat.Id == id
                    && (chat.User1.Id == currentUserId || chat.User2.Id == currentUserId),
                ct
            );

        return chat?.ToResponse();
    }
}
