using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("games")]
internal class Game
{
    public long Id { get; private set; }

    public string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public long AppliedEmbeddingVersion { get; private set; }

    public ICollection<GameTag> Tags = [];

    public Game(string name)
    {
        Name = name;
    }

    public void MarkEmbeddingApplied(long version)
    {
        if (version > AppliedEmbeddingVersion)
        {
            AppliedEmbeddingVersion = version;
        }
    }
}
