using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly INotificationService _notificationService;

    public FeedbackController(IFeedbackService feedbackService, INotificationService notificationService)
    {
        _feedbackService = feedbackService;
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitFeedback([FromBody] FeedbackDto dto)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Ok(ApiResponse<bool>.Fail("未认证", 401));
        }

        var type = dto.Type switch
        {
            "内容反馈" => FeedbackType.ContentFeedback,
            "体验反馈" => FeedbackType.ExperienceFeedback,
            _ => (FeedbackType?)null,
        };
        if (type == null)
        {
            return Ok(ApiResponse<bool>.Fail($"无效的反馈类型: {dto.Type}", 400));
        }

        try
        {
            await _feedbackService.SubmitFeedbackAsync(userId, type.Value, dto.Content);
            var preview = dto.Content.Length > 20
                ? dto.Content[..20] + "…"
                : dto.Content;
            await _notificationService.CreateAsync(userId,
                "反馈已提交",
                $"您的反馈「{preview}」已提交，请等待管理员处理。");
            return Ok(ApiResponse<bool>.Success(true, "反馈提交成功"));
        }
        catch (ArgumentException ex)
        {
            return Ok(ApiResponse<bool>.Fail(ex.Message, 400));
        }
    }
}