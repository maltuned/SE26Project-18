using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MessageResponse>> GetByChatIdAsync(long chatId, long userId, CancellationToken ct)
    {
        var chat = await _db.Chats.FindAsync([chatId], ct)
                   ?? throw new NotFoundException("Chat not found.");

        if (chat.User1.Id != userId && chat.User2.Id != userId)
            throw new ForbiddenException("You are not a participant in this chat.");

        var messages = await _db.Messages
            .Where(m => EF.Property<long>(m, "ChatId") == chatId) // shadow FK
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

        return messages.Select(m => new MessageResponse(m.Sender.Id, m.Content, m.SentAt)).ToList();
    }

    public async Task<MessageResponse> SendAsync(long chatId, long senderId, string content, CancellationToken ct)
    {
        var chat = await _db.Chats
                       .Include(c => c.User1).Include(c => c.User2)
                       .FirstOrDefaultAsync(c => c.Id == chatId, ct)
                   ?? throw new NotFoundException("Chat not found.");

        if (chat.User1.Id != senderId && chat.User2.Id != senderId)
            throw new ForbiddenException("You are not a participant in this chat.");

        if (chat.Status == Models.Enums.ChatStatus.Free)
        {
            // 开放聊天直接发送
        }

        var sender = await _db.Users.FindAsync([senderId], ct)
                     ?? throw new NotFoundException("User not found.");

        var message = new Message(sender, content, DateTime.UtcNow);
        chat.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        return new MessageResponse(sender.Id, content, message.SentAt);
    }
}
