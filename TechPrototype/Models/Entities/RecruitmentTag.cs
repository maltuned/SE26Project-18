using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("recruitment_tags")]
public class RecruitmentTag
{
    public long Id { get; private set; }

    public string Name { get; set; } = string.Empty;

    public RecruitmentTag(string name)
    {
        Name = name;
    }
}
