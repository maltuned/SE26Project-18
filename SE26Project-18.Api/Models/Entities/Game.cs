using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("games")]
internal class Game
{
    public long Id { get; private set; }

    public string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public ICollection<GameTag> Tags = [];

    public Game(string name)
    {
        Name = name;
    }

#pragma warning disable CS8618
    private Game() { } // EF Core
#pragma warning restore CS8618
}
