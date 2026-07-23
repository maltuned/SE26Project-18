using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; private init; }

    public User Responder { get; private init; }

    public ResponseType Type { get; private set; } = ResponseType.Pending;

    public Response(Recruitment recruitment, User responder)
    {
        Recruitment = recruitment;
        Responder = responder;
    }

    public void Accept()
    {
        EnsurePending();
        Type = ResponseType.Accepted;
    }

    public void Reject()
    {
        EnsurePending();
        Type = ResponseType.Rejected;
    }

    private void EnsurePending()
    {
        if (Type != ResponseType.Pending)
            throw new InvalidOperationException("Response has already been processed.");
    }
}
