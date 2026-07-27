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
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
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
            return Ok(ApiResponse<bool>.Success(true, "举报提交成功"));
        }
        catch (ArgumentException ex)
        {
            return Ok(ApiResponse<bool>.Fail(ex.Message, 400));
        }
    }
}