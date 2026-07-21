using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("refresh_tokens")]
public class RefreshToken
{
    public long Id { get; private set; }

    public string TokenHashed { get; private set; } = string.Empty;

    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    private RefreshToken() { }

    public RefreshToken(string tokenHashed, long userId, DateTime expiresAt)
    {
        TokenHashed = tokenHashed;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}