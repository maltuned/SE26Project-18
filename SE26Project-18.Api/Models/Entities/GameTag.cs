using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("game_tags")]
public class GameTag
{
    public long Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
