using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Dtos.Response;

public class CreateResponseDto
{
    [Required(ErrorMessage = "招募ID不能为空")]
    [Range(1, long.MaxValue, ErrorMessage = "招募ID无效")]
    public long RecruitmentId { get; set; }

    [Required(ErrorMessage = "打招呼内容不能为空")]
    [MinLength(1, ErrorMessage = "打招呼内容不能为空")]
    [MaxLength(200, ErrorMessage = "打招呼内容不能超过200字")]
    public string GreetingMessage { get; set; } = string.Empty;
}
