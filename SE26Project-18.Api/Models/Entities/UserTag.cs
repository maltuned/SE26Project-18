using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("user_tags")]
public class UserTag
{
    public long Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public UserTag(string name)
    {
        Name = name;
    }
}
