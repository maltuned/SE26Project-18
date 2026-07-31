using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("user_settings")]
public class UserSettings
{
    public long Id { get; private set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public bool PushEnabled { get; set; } = true;
    public bool ProfileVisible { get; set; } = true;
    public bool DarkMode { get; set; } = false;
}