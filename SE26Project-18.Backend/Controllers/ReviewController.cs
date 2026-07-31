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
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;

    public ReviewController(
        IReviewService reviewService,
        INotificationService notificationService,
        IUserService userService,
        IChatService chatService)
    {
        _reviewService = reviewService;
        _notificationService = notificationService;
        _userService = userService;
        _chatService = chatService;
    }

    private long GetUserId()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim?.Value, out var id) ? id : 0;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Ok(ApiResponse<bool>.Fail("未认证", 401));

        try
        {
            var chat = await _chatService.GetChatByUsersAsync([userId, dto.RevieweeId]);
            if (chat != null && chat.ChatStatus == "限制")
                return Ok(ApiResponse<bool>.Fail("聊天限制中，无法评价", 400));

            await _reviewService.CreateAsync(userId, dto.RevieweeId, dto.Content);
            var reviewer = await _userService.GetUserByIdAsync(userId);
            var reviewerName = reviewer?.Nickname ?? reviewer?.Username ?? "用户";
            await _notificationService.CreateAsync(dto.RevieweeId,
                "收到新评价",
                $"收到{reviewerName}的评价");
            return Ok(ApiResponse<bool>.Success(true, "评价成功"));
        }
        catch (ArgumentException ex)
        {
            return Ok(ApiResponse<bool>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetReviewsByUser(long userId)
    {
        var reviews = await _reviewService.GetReviewsForUserAsync(userId);
        return Ok(ApiResponse<List<ReviewDto>>.Success(reviews, "获取成功"));
    }

    [HttpGet("check/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> HasReviewed(long userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == 0)
            return Ok(ApiResponse<bool>.Fail("未认证", 401));

        var hasReviewed = await _reviewService.HasReviewedAsync(currentUserId, userId);
        return Ok(ApiResponse<bool>.Success(hasReviewed, "获取成功"));
    }

    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(long id, [FromBody] UpdateReviewStatusDto dto)
    {
        var status = dto.Status switch
        {
            "隐藏" => ReviewStatus.Hidden,
            "显示" => ReviewStatus.Visible,
            _ => (ReviewStatus?)null,
        };
        if (status == null)
            return Ok(ApiResponse<bool>.Fail("无效的状态", 400));

        var ok = await _reviewService.UpdateStatusAsync(id, status.Value);
        return ok
            ? Ok(ApiResponse<bool>.Success(true, "更新成功"))
            : Ok(ApiResponse<bool>.Fail("评价不存在", 404));
    }
}

public class UpdateReviewStatusDto
{
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}