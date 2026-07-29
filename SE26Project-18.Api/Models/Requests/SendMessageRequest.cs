using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record SendMessageRequest(
    [Required, StringLength(4000, MinimumLength = 1)] string Content
)
{
    public const int MaxContentLength = 4000;
}
