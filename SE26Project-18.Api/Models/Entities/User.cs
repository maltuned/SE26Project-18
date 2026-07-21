using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("users")]
public class User
{
    public long Id { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHashed { get; private set; } = string.Empty;

    public string Nickname { get; private set; } = string.Empty;

    public string Signature { get; private set; } = string.Empty;

    public Gender Gender { get; private set; }

    public UserStatus Status { get; private set; }

    public ICollection<UserTag> Tags { get; private set; } = [];

    public ICollection<Recruitment> Recruitments { get; private set; } = [];

    public ICollection<Chat> Chats { get; private set; } = [];
}
