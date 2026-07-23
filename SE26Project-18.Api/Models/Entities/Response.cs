using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }
    public Recruitment Recruitment { get; private set; }
    public User Responder { get; private set; }
    public User Recruiter { get; private set; }
    public string GreetingMessage { get; private set; } = string.Empty;
    public ResponseType Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long? ChatId { get; private set; }
    public Chat? Chat { get; private set; }
    [ConcurrencyCheck]
    public DateTime UpdatedAt { get; private set; }

    protected Response() { }

    public Response(Recruitment recruitment, User responder, User recruiter, string greetingMessage)
    {
        Recruitment = recruitment;
        Responder = responder;
        Recruiter = recruiter;
        GreetingMessage = greetingMessage;
        Status = ResponseType.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Accept() { Status = ResponseType.Accepted; UpdatedAt = DateTime.UtcNow; }
    public void Reject() { Status = ResponseType.Rejected; UpdatedAt = DateTime.UtcNow; }
    public void SetChat(Chat chat) { Chat = chat; UpdatedAt = DateTime.UtcNow; }
}
