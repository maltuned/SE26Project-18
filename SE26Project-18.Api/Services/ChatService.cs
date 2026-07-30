using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Pagination;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class ChatService : IChatService
{
    private const byte CursorPurpose = 1;

    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        this._db = db;
    }

    public async Task<CursorPagedResponse<ChatResponse>> GetChatsAsync(
        long userId,
        string? before,
        int limit,
        CancellationToken ct
    )
    {
        if (limit is < 1 or > 100)
        {
            throw new ValidationException("Limit must be between 1 and 100.");
        }

        var query = _db
            .Chats.AsNoTracking()
            .Include(chat => chat.Recruitment)
            .Include(chat => chat.User1)
            .Include(chat => chat.User2)
            .Where(chat => chat.User1.Id == userId || chat.User2.Id == userId);

        if (before is not null)
        {
            var cursor = CursorCodec.Decode(before, CursorPurpose);
            if (cursor.Timestamp is null)
            {
                query = query.Where(chat =>
                    !chat.Messages.Any() && chat.Id < cursor.Id
                );
            }
            else
            {
                query = query.Where(chat =>
                    !chat.Messages.Any()
                    || chat.Messages.Max(message => (DateTime?)message.SentAt) < cursor.Timestamp
                    || (
                        chat.Messages.Max(message => (DateTime?)message.SentAt) == cursor.Timestamp
                        && chat.Id < cursor.Id
                    )
                );
            }
        }

        var chats = await query
            .OrderByDescending(chat =>
                chat.Messages.Max(message => (DateTime?)message.SentAt)
            )
            .ThenByDescending(chat => chat.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = chats.Count > limit;
        if (hasMore)
        {
            chats.RemoveAt(limit);
        }

        var lastMessages = await GetLastMessagesAsync(chats.Select(chat => chat.Id).ToList(), ct);
        var items = chats
            .Select(chat => chat.ToResponse(lastMessages.GetValueOrDefault(chat.Id)))
            .ToList();
        var lastChat = chats.LastOrDefault();
        var lastActivity = lastChat is null
            ? null
            : lastMessages.GetValueOrDefault(lastChat.Id)?.SentAt;
        var nextCursor = hasMore && lastChat is not null
            ? CursorCodec.Encode(CursorPurpose, lastActivity, lastChat.Id)
            : null;

        return new CursorPagedResponse<ChatResponse>(items, nextCursor, hasMore);
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
            .FirstOrDefaultAsync(
                chat =>
                    (
                        (chat.User1.Id == currentUserId && chat.User2.Id == otherUserId)
                        || (chat.User1.Id == otherUserId && chat.User2.Id == currentUserId)
                    ),
                ct
            );

        if (chat is null)
        {
            return null;
        }

        var lastMessages = await GetLastMessagesAsync([chat.Id], ct);
        return chat.ToResponse(lastMessages.GetValueOrDefault(chat.Id));
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
            .FirstOrDefaultAsync(
                chat =>
                    chat.Id == id
                    && (chat.User1.Id == currentUserId || chat.User2.Id == currentUserId),
                ct
            );

        if (chat is null)
        {
            return null;
        }

        var lastMessages = await GetLastMessagesAsync([chat.Id], ct);
        return chat.ToResponse(lastMessages.GetValueOrDefault(chat.Id));
    }

    private async Task<Dictionary<long, MessageResponse>> GetLastMessagesAsync(
        IReadOnlyCollection<long> chatIds,
        CancellationToken ct
    )
    {
        if (chatIds.Count == 0)
        {
            return [];
        }

        return await _db
            .Messages.AsNoTracking()
            .Where(message => chatIds.Contains(EF.Property<long>(message, "ChatId")))
            .GroupBy(message => EF.Property<long>(message, "ChatId"))
            .Select(group => new
            {
                ChatId = group.Key,
                LastMessage = group
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => new MessageResponse(
                        message.Id,
                        message.Sender.Id,
                        message.Content,
                        message.SentAt
                    ))
                    .First(),
            })
            .ToDictionaryAsync(item => item.ChatId, item => item.LastMessage, ct);
    }
}
