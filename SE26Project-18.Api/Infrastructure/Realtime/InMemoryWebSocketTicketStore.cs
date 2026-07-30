using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace SE26Project_18.Api.Infrastructure.Realtime;

internal sealed class InMemoryWebSocketTicketStore : IWebSocketTicketStore
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(30);

    private readonly object _gate = new();

    private readonly Dictionary<string, TicketEntry> _tickets = [];

    private readonly Dictionary<(long UserId, long ChatId), string> _activeTickets = [];

    private DateTimeOffset _nextCleanup = DateTimeOffset.MinValue;

    private readonly TimeProvider _timeProvider;

    public InMemoryWebSocketTicketStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public WebSocketTicket Issue(long userId, long chatId)
    {
        var expiresAt = _timeProvider.GetUtcNow().Add(TicketLifetime);
        lock (_gate)
        {
            RemoveExpiredTickets();
            var owner = (userId, chatId);
            if (_activeTickets.Remove(owner, out var previousHash))
            {
                _tickets.Remove(previousHash);
            }

            string value;
            string hash;
            do
            {
                value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                hash = Hash(value);
            } while (_tickets.ContainsKey(hash));

            _tickets.Add(hash, new TicketEntry(userId, chatId, expiresAt));
            _activeTickets[owner] = hash;

            return new WebSocketTicket(value, expiresAt);
        }
    }

    public bool TryConsume(string ticket, long chatId, out long userId)
    {
        userId = default;
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return false;
        }

        lock (_gate)
        {
            var hash = Hash(ticket);
            if (!_tickets.Remove(hash, out var entry))
            {
                return false;
            }

            var owner = (entry.UserId, entry.ChatId);
            if (_activeTickets.TryGetValue(owner, out var activeHash) && activeHash == hash)
            {
                _activeTickets.Remove(owner);
            }

            if (entry.ChatId != chatId || entry.ExpiresAt <= _timeProvider.GetUtcNow())
            {
                return false;
            }

            userId = entry.UserId;
            return true;
        }
    }

    private void RemoveExpiredTickets()
    {
        var now = _timeProvider.GetUtcNow();
        if (now < _nextCleanup)
        {
            return;
        }

        var expiredHashes = _tickets
            .Where(ticket => ticket.Value.ExpiresAt <= now)
            .Select(ticket => ticket.Key)
            .ToList();
        foreach (var hash in expiredHashes)
        {
            if (_tickets.Remove(hash, out var ticket))
            {
                var owner = (ticket.UserId, ticket.ChatId);
                if (_activeTickets.TryGetValue(owner, out var activeHash) && activeHash == hash)
                {
                    _activeTickets.Remove(owner);
                }
            }
        }
        _nextCleanup = now.Add(TicketLifetime);
    }

    private static string Hash(string ticket)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ticket)));
    }

    private sealed record TicketEntry(long UserId, long ChatId, DateTimeOffset ExpiresAt);
}
