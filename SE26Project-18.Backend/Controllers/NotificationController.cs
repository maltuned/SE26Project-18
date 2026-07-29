using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetMyNotifications()
    {
        var userId = GetUserId();
        var notifications = await _notificationService.GetByUserIdAsync(userId);
        return Ok(ApiResponse<List<NotificationDto>>.Success(notifications));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<int>.Success(count));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(long id)
    {
        var userId = GetUserId();
        var result = await _notificationService.MarkAsReadAsync(id, userId);
        if (!result)
            return Ok(ApiResponse<bool>.Fail("通知不存在", 404));
        return Ok(ApiResponse<bool>.Success(true));
    }

    private long GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value
                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }
}