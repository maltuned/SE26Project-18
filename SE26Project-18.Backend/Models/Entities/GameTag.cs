using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("game_tags")]
public class GameTag
{
    public long Id { get; private set; }

    public string Name { get; set; } = string.Empty;

    public GameTag(string name)
    {
        Name = name;
    }
}
