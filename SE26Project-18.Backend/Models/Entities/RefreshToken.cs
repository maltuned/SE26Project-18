using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("refresh_tokens")]
public class RefreshToken
{
    public long Id { get; private set; }

    public long UserId { get; private init; }

    public User User { get; private init; } = null!;

    public string TokenHashed { get; private init; }

    public DateTime ExpiresAt { get; private init; }

    public bool IsRevoked { get; set; }

    public RefreshToken(long userId, string tokenHashed, DateTime expiresAt)
    {
        UserId = userId;
        TokenHashed = tokenHashed;
        ExpiresAt = expiresAt;
    }
}