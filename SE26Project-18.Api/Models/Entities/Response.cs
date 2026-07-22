using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; private init; }

    public User Responser { get; private init; }

    public ResponseType Type { get; private init; }

    public Response(Recruitment recruitment, User responser, ResponseType type)
    {
        Recruitment = recruitment;
        Responser = responser;
        Type = type;
    }
}
