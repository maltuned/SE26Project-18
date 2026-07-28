using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("recruitment_tags")]
internal class RecruitmentTag
{
    public long Id { get; private set; }

    public string Name { get; private init; }

    public RecruitmentTag(string name)
    {
        Name = name;
    }
}
