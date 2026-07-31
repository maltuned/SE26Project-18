using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("users")]
public class User
{
    public long Id { get; private set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long Uid => Id;

    public string Username { get; set; }

    public string PasswordHashed { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Other;

    public UserStatus Status { get; set; } = UserStatus.Normal;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long AppliedEmbeddingVersion { get; private set; }

    public ICollection<Recruitment> Recruitments { get; set; } = [];

    public UserSettings? Settings { get; set; }

    public ICollection<Chat> Chats { get; set; } = [];

    public ICollection<Response> SentResponses { get; set; } = [];

    public void MarkEmbeddingApplied(long version)
    {
        AppliedEmbeddingVersion = Math.Max(AppliedEmbeddingVersion, version);
    }

    [NotMapped]
    public IEnumerable<Response> ReceivedResponses =>
        Recruitments?.SelectMany(r => r.Responses ?? []) ?? [];

    public User(string username, string passwordHashed)
    {
        Username = username;
        PasswordHashed = passwordHashed;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
