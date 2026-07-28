using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Exceptions;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
internal class Response
{
    public long Id { get; private set; }

    public long RecruitmentId { get; private set; }

    public long ResponderId { get; private set; }

    public Recruitment Recruitment { get; private init; }

    public User Responder { get; private init; }

    public ResponseType Type { get; private set; } = ResponseType.Pending;

    [ConcurrencyCheck]
    public int Version { get; private set; }

    public Response(Recruitment recruitment, User responder)
    {
        Recruitment = recruitment;
        Responder = responder;
        RecruitmentId = recruitment.Id;
        ResponderId = responder.Id;
    }

    private Response()
    {
        Recruitment = null!;
        Responder = null!;
    }

    public void Accept()
    {
        EnsurePending();
        Type = ResponseType.Accepted;
        Version++;
    }

    public void Reject()
    {
        EnsurePending();
        Type = ResponseType.Rejected;
        Version++;
    }

    private void EnsurePending()
    {
        if (Type != ResponseType.Pending)
        {
            throw new ResponseAlreadyProcessedException(Type);
        }
    }
}
