using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; private set; }      // 关联招募

    public User Responder { get; private set; }              // 回应者

    public User Recruiter { get; private set; }              // 招募发布者

    public string GreetingMessage { get; private set; } = string.Empty;  // 打招呼内容

    public ResponseType Status { get; private set; }          // 审批状态

    public DateTime CreatedAt { get; private set; }           // 创建时间

    public long? ChatId { get; private set; }                // 关联聊天FK

    public Chat? Chat { get; private set; }                  // 接受后创建的聊天会话

    [ConcurrencyCheck]
    public DateTime UpdatedAt { get; private set; }           // 状态变更时间（并发标记）

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

    public void Accept()
    {
        Status = ResponseType.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = ResponseType.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetChat(Chat chat)
    {
        Chat = chat;
        UpdatedAt = DateTime.UtcNow;
    }
}
