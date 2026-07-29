using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MessageResponse>> GetHistoryAsync(
        long chatId,
        long userId,
        CancellationToken ct
    )
    {
        await using var transaction = await BeginTransactionAsync(ct);
        var chat = await GetChatAsync(chatId, ct);
        EnsureParticipant(chat, userId);
        if (chat.MarkAsRead(userId))
        {
            await _db.SaveChangesAsync(ct);
        }

        var messages = await _db
            .Messages.AsNoTracking()
            .Where(message => EF.Property<long>(message, "ChatId") == chatId)
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .Select(message => new MessageResponse(
                message.Sender.Id,
                message.Content,
                message.SentAt
            ))
            .ToListAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        return messages;
    }

    public async Task MarkAsReadAsync(long chatId, long userId, CancellationToken ct)
    {
        await using var transaction = await BeginTransactionAsync(ct);
        var chat = await GetChatAsync(chatId, ct);
        EnsureParticipant(chat, userId);
        if (chat.MarkAsRead(userId))
        {
            await _db.SaveChangesAsync(ct);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }
    }

    public async Task<MessageResponse> SendAsync(
        long chatId,
        long senderId,
        string content,
        CancellationToken ct
    )
    {
        content = content.Trim();
        if (content.Length == 0 || content.Length > SendMessageRequest.MaxContentLength)
        {
            throw new ValidationException(
                $"Content must contain between 1 and {SendMessageRequest.MaxContentLength} characters."
            );
        }

        await using var transaction = await BeginTransactionAsync(ct);
        var chat = await GetChatAsync(chatId, ct);
        EnsureParticipant(chat, senderId);

        var otherUserId = chat.User1.Id == senderId ? chat.User2.Id : chat.User1.Id;
        if (chat.Status == ChatStatus.Restricted)
        {
            var senderHasSent = await HasSentMessageAsync(chatId, senderId, ct);
            var otherUserHasSent = await HasSentMessageAsync(chatId, otherUserId, ct);
            if (senderHasSent && !otherUserHasSent)
            {
                throw new ConflictException("Wait for the other participant to reply.");
            }

            if (otherUserHasSent)
            {
                chat.Status = ChatStatus.Free;
            }
        }

        var sender = chat.User1.Id == senderId ? chat.User1 : chat.User2;
        var message = new Message(sender, content, DateTime.UtcNow);
        chat.Messages.Add(message);
        chat.RecordUnreadMessage(senderId);

        await _db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        return new MessageResponse(senderId, message.Content, message.SentAt);
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct)
    {
        return _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
    }

    private async Task<Chat> GetChatAsync(long chatId, CancellationToken ct)
    {
        return await _db
                .Chats.Include(chat => chat.User1)
                .Include(chat => chat.User2)
                .FirstOrDefaultAsync(chat => chat.Id == chatId, ct)
            ?? throw new NotFoundException("Chat not found.");
    }

    private Task<bool> HasSentMessageAsync(long chatId, long userId, CancellationToken ct)
    {
        return _db.Messages.AnyAsync(
            message =>
                EF.Property<long>(message, "ChatId") == chatId && message.Sender.Id == userId,
            ct
        );
    }

    private static void EnsureParticipant(Chat chat, long userId)
    {
        if (chat.User1.Id != userId && chat.User2.Id != userId)
        {
            throw new ForbiddenException("You are not a participant in this chat.");
        }
    }
}
