using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("games")]
public class Game
{
    public long Id { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public ICollection<GameTag> Tags = [];
}
