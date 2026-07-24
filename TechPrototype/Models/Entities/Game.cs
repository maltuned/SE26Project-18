using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("games")]
public class Game
{
    public long Id { get; private set; }

    public string Name { get; set; }

    public string Company { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Cover { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public ICollection<GameTag> Tags { get; set; } = [];

    public Game(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string company, string description, string cover, string icon)
    {
        Name = name;
        Company = company;
        Description = description;
        Cover = cover;
        Icon = icon;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTags(ICollection<GameTag> tags)
    {
        Tags = tags;
        UpdatedAt = DateTime.UtcNow;
    }
}