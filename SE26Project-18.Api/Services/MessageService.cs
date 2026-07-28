using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MessageResponse> SendAsync(
        long chatId,
        long senderId,
        long receiverId,
        string content,
        CancellationToken ct
    )
    {
        var chat =
            await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId, ct)
            ?? throw new NotFoundException("Chat not found.");

        if (chat.User1.Id != senderId && chat.User2.Id != senderId)
            throw new ForbiddenException("Sender is not a participant in this chat.");

        if (chat.User1.Id != receiverId && chat.User2.Id != receiverId)
            throw new ForbiddenException("Receiver is not a participant in this chat.");

        var sender =
            await _db.Users.FindAsync([senderId], ct)
            ?? throw new NotFoundException("Sender not found.");

        var receiver =
            await _db.Users.FindAsync([receiverId], ct)
            ?? throw new NotFoundException("Receiver not found.");

        var message = new Message(chat, sender, receiver, content, DateTime.UtcNow);

        // Update unread count for the receiver
        if (chat.User1.Id == receiverId)
            chat.NewMsgsCntForUser1++;
        else
            chat.NewMsgsCntForUser2++;

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        return message.ToResponse();
    }

    public async Task<IReadOnlyList<MessageResponse>> GetByChatAsync(
        long chatId,
        long userId,
        CancellationToken ct
    )
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId, ct);
        if (chat is null)
            return [];

        if (chat.User1.Id != userId && chat.User2.Id != userId)
            throw new ForbiddenException("You are not a participant in this chat.");

        var messages = await _db
            .Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.Chat.Id == chatId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

        return messages.Select(m => m.ToResponse()).ToList();
    }
}
