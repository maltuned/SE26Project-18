using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; private set; }

    public ResponseType Status { get; private set; }

    public Response(Recruitment recruitment)
    {
        Recruitment = recruitment;
    }
}
