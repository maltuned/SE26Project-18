using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IReportService _reportService;
    private readonly IFeedbackService _feedbackService;
    private readonly IUserService _userService;
    private readonly IRecruitmentService _recruitmentService;
    private readonly IGameService _gameService;
    private readonly IChatService _chatService;
    private readonly IMessageService _messageService;
    private readonly INotificationService _notificationService;

    public AdminController(
        IAdminService adminService,
        IReportService reportService,
        IFeedbackService feedbackService,
        IUserService userService,
        IRecruitmentService recruitmentService,
        IGameService gameService,
        IChatService chatService,
        IMessageService messageService,
        INotificationService notificationService)
    {
        _adminService = adminService;
        _reportService = reportService;
        _feedbackService = feedbackService;
        _userService = userService;
        _recruitmentService = recruitmentService;
        _gameService = gameService;
        _chatService = chatService;
        _messageService = messageService;
        _notificationService = notificationService;
    }

    private long GetAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return long.Parse(idClaim!);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] AdminLoginRequest request)
    {
        try
        {
            var (token, admin) = await _adminService.LoginAsync(request.Username, request.Password);
            return Ok(ApiResponse<object>.Success(new
            {
                token,
                admin = new { admin.Id, admin.Username }
            }, "登录成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<object>.Fail(ex.Message, 401));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending-count")]
    public async Task<ActionResult<ApiResponse<object>>> GetPendingCount()
    {
        var counts = await _adminService.GetPendingCountAsync();
        return Ok(ApiResponse<object>.Success(new
        {
            pending_reports = counts[0],
            pending_feedbacks = counts[1],
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<List<Report>>>> GetAllReports([FromQuery] string? status)
    {
        ReportStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status))
            statusFilter = status.ToReportStatus();

        var reports = await _reportService.GetAllAsync(statusFilter);
        return Ok(ApiResponse<List<Report>>.Success(reports));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("reports/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> HandleReport(long id, [FromBody] HandleReportRequest request)
    {
        var adminId = GetAdminId();
        var reportStatus = request.Status.ToReportStatus();
        var result = await _reportService.UpdateStatusAsync(id, reportStatus, adminId);
        if (!result)
            return Ok(ApiResponse<bool>.Fail("举报不存在", 404));

        var report = await _reportService.GetByIdAsync(id);
        if (report != null)
        {
            var targetName = await ResolveTargetNameAsync(report.TargetType, report.TargetId);
            var targetTypeText = report.TargetType switch
            {
                ReportTargetType.Recruitment => "招募",
                ReportTargetType.User => "用户",
                ReportTargetType.Chat => "聊天",
                _ => report.TargetType.ToString()
            };
            var statusText = reportStatus switch
            {
                ReportStatus.Resolved => "处理",
                ReportStatus.Rejected => "驳回",
                _ => reportStatus.ToString()
            };
            await _notificationService.CreateAsync(report.ReporterId,
                "举报处理结果",
                $"您对{targetTypeText}「{targetName}」的举报已被{statusText}");
        }

        return Ok(ApiResponse<bool>.Success(true, "处理成功"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("reports/{reportId}/target")]
    public async Task<ActionResult<ApiResponse<object>>> GetReportTarget(long reportId)
    {
        var report = await _reportService.GetByIdAsync(reportId);
        if (report == null)
            return Ok(ApiResponse<object>.Fail("举报不存在", 404));

        object target = report.TargetType switch
        {
            ReportTargetType.Recruitment => await _recruitmentService.GetRecruitmentByIdAsync(report.TargetId)
                is RecruitmentDetailDto r ? r : "招募不存在",
            ReportTargetType.User => await _userService.GetUserByIdAsync(report.TargetId)
                is UserDto u ? u : "用户不存在",
            ReportTargetType.Chat => await GetChatTargetAsync(report.TargetId),
            _ => new { report.TargetId, report.TargetType }
        };

        return Ok(ApiResponse<object>.Success(new
        {
            report.TargetId,
            report.TargetType,
            target
        }));
    }

    private async Task<object> GetChatTargetAsync(long chatId)
    {
        var chat = await _chatService.GetChatByIdAsync(chatId, 0);
        if (chat == null) return "聊天不存在";

        var messages = await _messageService.GetMessagesByChatAsync(chatId);

        return new
        {
            chat.Id,
            chat.RecruitmentTitle,
            chat.ChatStatus,
            Participant = chat.OtherUser,
            Messages = messages.Select(m => new
            {
                m.Id,
                m.Content,
                m.CreatedAt,
                Sender = m.Sender?.Nickname ?? m.Sender?.Username ?? "",
            }).ToList()
        };
    }

    private async Task<string> ResolveTargetNameAsync(ReportTargetType targetType, long targetId)
    {
        return targetType switch
        {
            ReportTargetType.Recruitment => (await _recruitmentService.GetRecruitmentByIdAsync(targetId))?.Title ?? "招募",
            ReportTargetType.User => (await _userService.GetUserByIdAsync(targetId))?.Nickname ?? "用户",
            ReportTargetType.Chat => (await _chatService.GetChatByIdAsync(targetId, 0))?.OtherUser?.Nickname ?? "用户",
            _ => targetType.ToString()
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("feedbacks")]
    public async Task<ActionResult<ApiResponse<List<Feedback>>>> GetAllFeedbacks([FromQuery] string? status)
    {
        FeedbackStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status))
            statusFilter = status.ToFeedbackStatus();

        var feedbacks = await _feedbackService.GetAllAsync(statusFilter);
        return Ok(ApiResponse<List<Feedback>>.Success(feedbacks));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("feedbacks/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> HandleFeedback(long id, [FromBody] HandleFeedbackRequest request)
    {
        var adminId = GetAdminId();
        var result = await _feedbackService.UpdateStatusAsync(id, request.Status.ToFeedbackStatus(), adminId);
        if (!result)
            return Ok(ApiResponse<bool>.Fail("反馈不存在", 404));

        var feedback = await _feedbackService.GetByIdAsync(id);
        if (feedback != null)
        {
            var preview = feedback.Content.Length > 20
                ? feedback.Content[..20] + "…"
                : feedback.Content;
            await _notificationService.CreateAsync(feedback.UserId,
                "反馈处理结果",
                $"您的反馈「{preview}」已被处理，感谢您的宝贵意见！");
        }

        return Ok(ApiResponse<bool>.Success(true, "处理成功"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> SearchUsers([FromQuery] string? search)
    {
        if (string.IsNullOrEmpty(search))
            return Ok(ApiResponse<List<UserDto>>.Success(await _userService.GetUsersAsync()));

        var users = await _userService.SearchUsersAsync(search);
        return Ok(ApiResponse<List<UserDto>>.Success(users));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id}/status")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserStatus(long id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userService.UpdateUserStatusAsync(id, request.Status.ToUserStatus());
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));
        return Ok(ApiResponse<UserDto>.Success(user, "更新成功"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(long id, [FromBody] Dictionary<string, object> data)
    {
        var user = await _userService.UpdateUserAsync(id, data);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));
        return Ok(ApiResponse<UserDto>.Success(user, "更新成功"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id}/clear")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ClearUserProfile(long id)
    {
        var user = await _userService.ClearUserProfileAsync(id);
        if (user == null)
            return Ok(ApiResponse<UserDto>.Fail("用户不存在", 404));
        return Ok(ApiResponse<UserDto>.Success(user, "已清空资料"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("recruitments")]
    public async Task<ActionResult<ApiResponse<List<RecruitmentDetailDto>>>> SearchRecruitments([FromQuery] string? search)
    {
        var recruitments = await _recruitmentService.SearchRecruitmentsAsync(search ?? "");
        return Ok(ApiResponse<List<RecruitmentDetailDto>>.Success(recruitments));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("recruitments/{id}/status")]
    public async Task<ActionResult<ApiResponse<RecruitmentDetailDto>>> CloseRecruitment(long id)
    {
        var data = new Dictionary<string, object> { { "status", "已关闭" } };
        var result = await _recruitmentService.UpdateRecruitmentAsync(id, data);
        if (result == null)
            return Ok(ApiResponse<RecruitmentDetailDto>.Fail("招募不存在", 404));
        return Ok(ApiResponse<RecruitmentDetailDto>.Success(result, "已关闭"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("recruitments/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRecruitment(long id)
    {
        var ok = await _recruitmentService.DeleteRecruitmentAsync(id);
        if (!ok)
            return Ok(ApiResponse<object>.Fail("招募不存在", 404));
        return Ok(ApiResponse<object>.Success(new { }, "已删除"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("games")]
    public async Task<ActionResult<ApiResponse<List<GameDto>>>> SearchGames([FromQuery] string? search)
    {
        var games = await _gameService.GetGamesAsync(search ?? "");
        return Ok(ApiResponse<List<GameDto>>.Success(games));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("games")]
    public async Task<ActionResult<ApiResponse<GameDto>>> CreateGame([FromBody] GameRequestDto request)
    {
        try
        {
            var game = await _gameService.CreateGameAsync(request);
            return Ok(ApiResponse<GameDto>.Success(game, "创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<GameDto>.Fail(ex.Message, 404));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("games/{id}")]
    public async Task<ActionResult<ApiResponse<GameDto>>> UpdateGame(long id, [FromBody] GameRequestDto request)
    {
        try
        {
            var game = await _gameService.UpdateGameAsync(id, request);
            return Ok(ApiResponse<GameDto>.Success(game, "更新成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Ok(ApiResponse<GameDto>.Fail(ex.Message, 404));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("games/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteGame(long id)
    {
        var ok = await _gameService.DeleteGameAsync(id);
        if (!ok)
            return Ok(ApiResponse<object>.Fail("游戏不存在", 404));
        return Ok(ApiResponse<object>.Success(new { }, "已删除"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("notifications")]
    public async Task<ActionResult<ApiResponse<object>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return Ok(ApiResponse<object>.Fail("标题和内容不能为空", 400));

        if (request.UserId.HasValue)
        {
            await _notificationService.CreateAsync(request.UserId.Value, request.Title, request.Body);
            return Ok(ApiResponse<object>.Success(new { }, "通知已发送"));
        }

        var allUsers = await _userService.GetUsersAsync();
        foreach (var user in allUsers)
        {
            await _notificationService.CreateAsync(user.Id, request.Title, request.Body);
        }
        return Ok(ApiResponse<object>.Success(new { }, $"已向 {allUsers.Count} 位用户发送通知"));
    }
}

public class AdminLoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class HandleReportRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class HandleFeedbackRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}