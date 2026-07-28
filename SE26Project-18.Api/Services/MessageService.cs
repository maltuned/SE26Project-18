using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class MessageService : IMessageService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<long, List<WebSocket>> _chatSockets = new();

    public MessageService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<MessageResponse>> GetHistoryAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var chat = await db.Chats.FindAsync([chatId], ct)
                   ?? throw new NotFoundException("Chat not found.");
        if (chat.User1.Id != userId && chat.User2.Id != userId)
            throw new ForbiddenException("Not a participant.");

        return await db.Messages
            .Where(m => EF.Property<long>(m, "ChatId") == chatId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageResponse(m.Sender.Id, m.Content, m.SentAt))
            .ToListAsync(ct);
    }

    public async Task<MessageResponse> SaveAndBroadcastAsync(long chatId, long senderId, string content, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var chat = await db.Chats.Include(c => c.User1).Include(c => c.User2)
                       .FirstOrDefaultAsync(c => c.Id == chatId, ct)
                   ?? throw new NotFoundException("Chat not found.");
        if (chat.User1.Id != senderId && chat.User2.Id != senderId)
            throw new ForbiddenException("Not a participant.");

        var sender = await db.Users.FindAsync([senderId], ct)
                     ?? throw new NotFoundException("User not found.");

        var message = new Message(sender, content, DateTime.UtcNow);
        chat.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        var resp = new MessageResponse(sender.Id, content, message.SentAt);
        await BroadcastAsync(chatId, resp);
        return resp;
    }

    public void AddSocket(long chatId, WebSocket socket)
    {
        _chatSockets.AddOrUpdate(chatId,
            _ => new List<WebSocket> { socket },
            (_, list) => { lock (list) { list.Add(socket); } return list; });
    }

    public void RemoveSocket(long chatId, WebSocket socket)
    {
        if (_chatSockets.TryGetValue(chatId, out var list))
            lock (list) { list.Remove(socket); }
    }

    private async Task BroadcastAsync(long chatId, MessageResponse msg)
    {
        if (!_chatSockets.TryGetValue(chatId, out var list)) return;

        var json = JsonSerializer.Serialize(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        var dead = new List<WebSocket>();

        lock (list)
        {
            foreach (var ws in list)
            {
                if (ws.State == WebSocketState.Open)
                    _ = SendAsync(ws, bytes);
                else
                    dead.Add(ws);
            }
            foreach (var d in dead) list.Remove(d);
        }
    }

    private static async Task SendAsync(WebSocket ws, byte[] data)
    {
        try
        {
            await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch { }
    }

    public void Dispose()
    {
        foreach (var (_, list) in _chatSockets)
            lock (list) { foreach (var ws in list) ws.Dispose(); }
        _chatSockets.Clear();
    }
}
