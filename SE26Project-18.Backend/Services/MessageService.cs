using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Hubs;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageService(AppDbContext db, MapperService mapper, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _mapper = mapper;
        _hubContext = hubContext;
    }

    IQueryable<Message> Query()
    {
        return _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver);
    }

    public async Task<List<MessageDto>> GetMessagesByChatAsync(long chatId)
    {
        var messages = await Query()
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        return messages.Select(_mapper.ToMessageDto).ToList();
    }

    public async Task<MessageDto> SendMessageAsync(long chatId, long senderId, long receiverId, string content)
    {
        var sender = await _db.Users.FindAsync(senderId)
            ?? throw new KeyNotFoundException("发送者不存在");
        var receiver = await _db.Users.FindAsync(receiverId)
            ?? throw new KeyNotFoundException("接收者不存在");
        var chat = await _db.Chats.FindAsync(chatId)
            ?? throw new KeyNotFoundException("聊天不存在");

        // If chat is closed, reject
        if (chat.ChatStatus == ChatStatus.Closed)
            throw new InvalidOperationException("聊天已关闭，无法发送消息");

        // Check message history in restricted chat
        bool senderHasSent = await _db.Messages.AnyAsync(m => m.ChatId == chatId && m.SenderId == senderId);
        bool receiverHasSent = await _db.Messages.AnyAsync(m => m.ChatId == chatId && m.SenderId == receiverId);

        // 限制状态下：己方发过且对方没发过 → 拦截
        if (chat.ChatStatus == ChatStatus.Restricted && senderHasSent && !receiverHasSent)
            throw new InvalidOperationException("等待对方回复中，暂不能发送消息");

        var message = new Message(content)
        {
            ChatId = chatId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Sender = sender,
            Receiver = receiver,
        };

        chat.NewMessageAt = DateTime.UtcNow;
        chat.UpdatedAt = DateTime.UtcNow;

        // 限制状态下，如果对方已发过消息，则开放聊天
        if (chat.ChatStatus == ChatStatus.Restricted && receiverHasSent)
        {
            chat.ChatStatus = ChatStatus.Open;
        }

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var dto = _mapper.ToMessageDto(message);
        await _hubContext.Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", dto);
        await _hubContext.Clients.Group($"user_{receiverId}").SendAsync("NewChatMessage", dto);

        return dto;
    }

    public async Task MarkAsReadAsync(long chatId, long userId)
    {
        await _db.Messages
            .Where(m => m.ChatId == chatId && m.ReceiverId == userId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }
}