using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("users")]
internal class User
{
    public long Id { get; private set; }

    public string Username { get; set; }

    public string PasswordHashed { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Other;

    public UserStatus Status { get; set; } = UserStatus.Online;

    public UserRole Role { get; private init; }

    public long AppliedEmbeddingVersion { get; private set; }

    public ICollection<UserTag> Tags { get; set; } = [];

    public ICollection<Recruitment> Recruitments { get; set; } = [];

    public ICollection<Chat> ChatsAsUser1 { get; set; } = [];

    public ICollection<Chat> ChatsAsUser2 { get; set; } = [];

    public User(string username, string passwordHashed, UserRole role)
    {
        Username = username;
        PasswordHashed = passwordHashed;
        Role = role;
    }

    public void MarkEmbeddingApplied(long version)
    {
        if (version > AppliedEmbeddingVersion)
            AppliedEmbeddingVersion = version;
    }
}
