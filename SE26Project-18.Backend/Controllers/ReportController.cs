using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly INotificationService _notificationService;
    private readonly IRecruitmentService _recruitmentService;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;

    public ReportController(
        IReportService reportService,
        INotificationService notificationService,
        IRecruitmentService recruitmentService,
        IUserService userService,
        IChatService chatService)
    {
        _reportService = reportService;
        _notificationService = notificationService;
        _recruitmentService = recruitmentService;
        _userService = userService;
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitReport([FromBody] ReportDto dto)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return Ok(ApiResponse<bool>.Fail("未认证", 401));
        }

        var targetType = dto.TargetType switch
        {
            "招募" => ReportTargetType.Recruitment,
            "用户" => ReportTargetType.User,
            "聊天" => ReportTargetType.Chat,
            _ => (ReportTargetType?)null,
        };
        if (targetType == null)
        {
            return Ok(ApiResponse<bool>.Fail($"无效的举报目标类型: {dto.TargetType}", 400));
        }

        var violationType = dto.ViolationType switch
        {
            "涉政" => ViolationType.Political,
            "谩骂" => ViolationType.Abuse,
            "广告" => ViolationType.Advertisement,
            "色情" => ViolationType.Pornography,
            "欺诈" => ViolationType.Fraud,
            "其他" => ViolationType.Other,
            _ => (ViolationType?)null,
        };
        if (violationType == null)
        {
            return Ok(ApiResponse<bool>.Fail($"无效的违规类型: {dto.ViolationType}", 400));
        }

        try
        {
            await _reportService.SubmitReportAsync(userId, targetType.Value, dto.TargetId, violationType.Value, dto.Content);
            var targetName = await ResolveTargetNameAsync(targetType.Value, dto.TargetId);
            await _notificationService.CreateAsync(userId,
                "举报已提交",
                $"您对{dto.TargetType}「{targetName}」的举报（{dto.ViolationType}）已提交，请等待管理员处理。");
            return Ok(ApiResponse<bool>.Success(true, "举报提交成功"));
        }
        catch (ArgumentException ex)
        {
            return Ok(ApiResponse<bool>.Fail(ex.Message, 400));
        }
    }

    private async Task<string> ResolveTargetNameAsync(ReportTargetType targetType, long targetId)
    {
        return targetType switch
        {
            ReportTargetType.Recruitment => (await _recruitmentService.GetRecruitmentByIdAsync(targetId))?.Title ?? "招募",
            ReportTargetType.User => (await _userService.GetUserByIdAsync(targetId))?.Nickname ?? "用户",
            ReportTargetType.Chat => await ResolveChatTargetNameAsync(targetId),
            _ => targetType.ToString()
        };
    }

    private async Task<string> ResolveChatTargetNameAsync(long chatId)
    {
        var chat = await _chatService.GetChatByIdAsync(chatId, 0);
        return chat?.OtherUser?.Nickname ?? "用户";
    }
}