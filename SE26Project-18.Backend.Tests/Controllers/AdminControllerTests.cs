using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _admin = new();
    private readonly Mock<IReportService> _report = new();
    private readonly Mock<IFeedbackService> _feedback = new();
    private readonly Mock<IUserService> _user = new();
    private readonly Mock<IRecruitmentService> _recruit = new();
    private readonly Mock<IGameService> _game = new();
    private readonly Mock<IChatService> _chat = new();
    private readonly Mock<IMessageService> _msg = new();
    private readonly Mock<INotificationService> _notif = new();
    private readonly Mock<IReviewService> _review = new();
    private readonly Mock<ITagService> _tag = new();

    private AdminController CreateController()
    {
        var c = new AdminController(_admin.Object, _report.Object, _feedback.Object,
            _user.Object, _recruit.Object, _game.Object, _chat.Object, _msg.Object,
            _notif.Object, _review.Object, _tag.Object);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return c;
    }

    // ==================== Login ====================
    [Fact]
    public async Task Login_ReturnsOk_OnSuccess()
    {
        var admin = new Admin("admin", "hash");
        typeof(Admin).GetProperty("Id")!.SetValue(admin, 1L);
        _admin.Setup(a => a.LoginAsync("admin", "123456")).ReturnsAsync(("token", admin));
        var c = CreateController();

        var result = await c.Login(new AdminLoginRequest { Username = "admin", Password = "123456" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsApiFail_OnError()
    {
        _admin.Setup(a => a.LoginAsync("bad", "pw")).ThrowsAsync(new InvalidOperationException("密码错误"));
        var c = CreateController();

        var result = await c.Login(new AdminLoginRequest { Username = "bad", Password = "pw" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.Equal(401, api.Status);
    }

    // ==================== PendingCount ====================
    [Fact]
    public async Task GetPendingCount_ReturnsOk()
    {
        _admin.Setup(a => a.GetPendingCountAsync()).ReturnsAsync(new[] { 3, 5 });
        var c = CreateController();

        var result = await c.GetPendingCount();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== Reports ====================
    [Fact]
    public async Task GetAllReports_ReturnsOk()
    {
        _report.Setup(r => r.GetAllAsync(null)).ReturnsAsync(new List<Report>());
        var c = CreateController();

        var result = await c.GetAllReports(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task HandleReport_ReturnsFail_WhenNotFound()
    {
        _report.Setup(r => r.UpdateStatusAsync(99, It.IsAny<ReportStatus>(), 1)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.HandleReport(99, new HandleReportRequest { Status = "已处理" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task GetReportTarget_ReturnsFail_WhenReportNotFound()
    {
        _report.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Report?)null);
        var c = CreateController();

        var result = await c.GetReportTarget(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    // ==================== Feedbacks ====================
    [Fact]
    public async Task GetAllFeedbacks_ReturnsOk()
    {
        _feedback.Setup(f => f.GetAllAsync(null)).ReturnsAsync(new List<Feedback>());
        var c = CreateController();

        var result = await c.GetAllFeedbacks(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task HandleFeedback_ReturnsFail_WhenNotFound()
    {
        _feedback.Setup(f => f.UpdateStatusAsync(99, It.IsAny<FeedbackStatus>(), 1)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.HandleFeedback(99, new HandleFeedbackRequest { Status = "已处理" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    // ==================== Users ====================
    [Fact]
    public async Task SearchUsers_ReturnsAll_WhenNoId()
    {
        _user.Setup(u => u.GetUsersAsync()).ReturnsAsync(new List<UserDto>());
        var c = CreateController();

        var result = await c.SearchUsers(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchUsers_ReturnsById_WhenIdProvided()
    {
        _user.Setup(u => u.GetUserByIdAsync(5)).ReturnsAsync(new UserDto { Id = 5, Username = "u5" });
        var c = CreateController();

        var result = await c.SearchUsers(5);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUserStatus_ReturnsFail_WhenUserNotFound()
    {
        _user.Setup(u => u.UpdateUserStatusAsync(99, It.IsAny<UserStatus>())).ReturnsAsync((UserDto?)null);
        var c = CreateController();

        var result = await c.UpdateUserStatus(99, new UpdateUserStatusRequest { Status = "封禁" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<UserDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task UpdateUser_ReturnsFail_WhenUserNotFound()
    {
        _user.Setup(u => u.UpdateUserAsync(99, It.IsAny<Dictionary<string, object>>())).ReturnsAsync((UserDto?)null);
        var c = CreateController();

        var result = await c.UpdateUser(99, new Dictionary<string, object> { { "nickname", "x" } });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<UserDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task ClearUserProfile_ReturnsFail_WhenUserNotFound()
    {
        _user.Setup(u => u.ClearUserProfileAsync(99)).ReturnsAsync((UserDto?)null);
        var c = CreateController();

        var result = await c.ClearUserProfile(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<UserDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    // ==================== Recruitments ====================
    [Fact]
    public async Task SearchRecruitments_ReturnsAll_WhenNoId()
    {
        _recruit.Setup(r => r.SearchRecruitmentsAsync("")).ReturnsAsync(new List<RecruitmentDetailDto>());
        var c = CreateController();

        var result = await c.SearchRecruitments(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CloseRecruitment_ReturnsFail_WhenNotFound()
    {
        _recruit.Setup(r => r.UpdateRecruitmentAsync(99, It.IsAny<Dictionary<string, object>>())).ReturnsAsync((RecruitmentDetailDto?)null);
        var c = CreateController();

        var result = await c.CloseRecruitment(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<RecruitmentDetailDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task DeleteRecruitment_ReturnsFail_WhenNotFound()
    {
        _recruit.Setup(r => r.DeleteRecruitmentAsync(99)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.DeleteRecruitment(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    // ==================== Games ====================
    [Fact]
    public async Task SearchGames_ReturnsAll_WhenNoId()
    {
        _game.Setup(g => g.GetGamesAsync("")).ReturnsAsync(new List<GameDto>());
        var c = CreateController();

        var result = await c.SearchGames(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateGame_ReturnsOk()
    {
        _game.Setup(g => g.CreateGameAsync(It.IsAny<GameRequestDto>())).ReturnsAsync(new GameDto { Id = 1, Name = "New" });
        var c = CreateController();

        var result = await c.CreateGame(new GameRequestDto { Name = "New" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGame_ReturnsApiFail_WhenNotFound()
    {
        _game.Setup(g => g.UpdateGameAsync(99, It.IsAny<GameRequestDto>())).ThrowsAsync(new KeyNotFoundException());
        var c = CreateController();

        var result = await c.UpdateGame(99, new GameRequestDto { Name = "X" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<GameDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task DeleteGame_ReturnsFail_WhenNotFound()
    {
        _game.Setup(g => g.DeleteGameAsync(99)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.DeleteGame(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    // ==================== Game Tags ====================
    [Fact]
    public async Task GetGameTags_ReturnsOk()
    {
        _tag.Setup(t => t.GetGameTagsAsync()).ReturnsAsync(new List<GameTagDto>());
        var c = CreateController();

        var result = await c.GetGameTags();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateGameTag_ReturnsOk()
    {
        _tag.Setup(t => t.CreateGameTagAsync("RPG")).ReturnsAsync(new GameTagDto { Id = 1, Name = "RPG" });
        var c = CreateController();

        var result = await c.CreateGameTag(new CreateTagRequest { Name = "RPG" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGameTag_ReturnsFail_WhenNotFound()
    {
        _tag.Setup(t => t.UpdateGameTagAsync(99, "X")).ReturnsAsync((GameTagDto?)null);
        var c = CreateController();

        var result = await c.UpdateGameTag(99, new CreateTagRequest { Name = "X" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<GameTagDto>>(ok.Value);
        Assert.Equal(404, api.Status);
    }

    [Fact]
    public async Task DeleteGameTag_ReturnsOk()
    {
        _tag.Setup(t => t.DeleteGameTagAsync(1)).ReturnsAsync(true);
        var c = CreateController();

        var result = await c.DeleteGameTag(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== Recruitment Tags ====================
    [Fact]
    public async Task GetRecruitmentTags_ReturnsOk()
    {
        _tag.Setup(t => t.GetRecruitmentTagsAsync()).ReturnsAsync(new List<RecruitmentTagDto>());
        var c = CreateController();

        var result = await c.GetRecruitmentTags();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRecruitmentTag_ReturnsOk()
    {
        _tag.Setup(t => t.CreateRecruitmentTagAsync("Casual")).ReturnsAsync(new RecruitmentTagDto { Id = 1, Name = "Casual" });
        var c = CreateController();

        var result = await c.CreateRecruitmentTag(new CreateTagRequest { Name = "Casual" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== Notifications ====================
    [Fact]
    public async Task SendNotification_ReturnsFail_WhenEmptyTitle()
    {
        var c = CreateController();

        var result = await c.SendNotification(new SendNotificationRequest { UserId = 1, Title = "", Body = "" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.Equal(400, api.Status);
    }

    [Fact]
    public async Task SendNotification_ToAllUsers_WhenNoUserId()
    {
        _user.Setup(u => u.GetUsersAsync()).ReturnsAsync(new List<UserDto> { new() { Id = 1 } });
        var c = CreateController();

        var result = await c.SendNotification(new SendNotificationRequest { Title = "Announce", Body = "Hello all" });

        Assert.IsType<OkObjectResult>(result.Result);
        _notif.Verify(n => n.CreateAsync(1, "Announce", "Hello all"), Times.Once);
    }

    // ==================== HandleReport success ====================
    [Fact]
    public async Task HandleReport_Succeeds_WithNotification()
    {
        _report.Setup(r => r.UpdateStatusAsync(1, It.IsAny<ReportStatus>(), 1)).ReturnsAsync(true);
        var report = new Report { ReporterId = 2, TargetType = ReportTargetType.User, TargetId = 3, ViolationType = ViolationType.Abuse, Content = "x" };
        _report.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _user.Setup(u => u.GetUserByIdAsync(3)).ReturnsAsync(new UserDto { Id = 3, Nickname = "BadUser" });
        var c = CreateController();

        var result = await c.HandleReport(1, new HandleReportRequest { Status = "已处理" });

        Assert.IsType<OkObjectResult>(result.Result);
        _notif.Verify(n => n.CreateAsync(2, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ==================== HandleFeedback success ====================
    [Fact]
    public async Task HandleFeedback_Succeeds_WithNotification()
    {
        _feedback.Setup(f => f.UpdateStatusAsync(1, It.IsAny<FeedbackStatus>(), 1)).ReturnsAsync(true);
        var fb = new Feedback { UserId = 2, Type = FeedbackType.ContentFeedback, Content = "Short feedback text" };
        _feedback.Setup(f => f.GetByIdAsync(1)).ReturnsAsync(fb);
        var c = CreateController();

        var result = await c.HandleFeedback(1, new HandleFeedbackRequest { Status = "已处理" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== UpdateUserStatus success ====================
    [Fact]
    public async Task UpdateUserStatus_Succeeds()
    {
        _user.Setup(u => u.UpdateUserStatusAsync(1, UserStatus.Banned))
            .ReturnsAsync(new UserDto { Id = 1, Username = "u", Status = "封禁" });
        var c = CreateController();

        var result = await c.UpdateUserStatus(1, new UpdateUserStatusRequest { Status = "封禁" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== UpdateUser success ====================
    [Fact]
    public async Task UpdateUser_Succeeds()
    {
        _user.Setup(u => u.UpdateUserAsync(1, It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(new UserDto { Id = 1, Nickname = "Updated" });
        var c = CreateController();

        var result = await c.UpdateUser(1, new Dictionary<string, object> { { "nickname", "Updated" } });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== ClearUserProfile success ====================
    [Fact]
    public async Task ClearUserProfile_Succeeds()
    {
        _user.Setup(u => u.ClearUserProfileAsync(1)).ReturnsAsync(new UserDto { Id = 1 });
        var c = CreateController();

        var result = await c.ClearUserProfile(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== CloseRecruitment success ====================
    [Fact]
    public async Task CloseRecruitment_Succeeds()
    {
        _recruit.Setup(r => r.UpdateRecruitmentAsync(1, It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(new RecruitmentDetailDto { Id = 1 });
        var c = CreateController();

        var result = await c.CloseRecruitment(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== DeleteGame success ====================
    [Fact]
    public async Task DeleteGame_Succeeds()
    {
        _game.Setup(g => g.DeleteGameAsync(1)).ReturnsAsync(true);
        var c = CreateController();

        var result = await c.DeleteGame(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== DeleteRecruitment success ====================
    [Fact]
    public async Task DeleteRecruitment_Succeeds()
    {
        _recruit.Setup(r => r.DeleteRecruitmentAsync(1)).ReturnsAsync(true);
        var c = CreateController();

        var result = await c.DeleteRecruitment(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== UpdateGameTag success ====================
    [Fact]
    public async Task UpdateGameTag_Succeeds()
    {
        _tag.Setup(t => t.UpdateGameTagAsync(1, "Updated")).ReturnsAsync(new GameTagDto { Id = 1, Name = "Updated" });
        var c = CreateController();

        var result = await c.UpdateGameTag(1, new CreateTagRequest { Name = "Updated" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ==================== Reviews ====================
    [Fact]
    public async Task GetAllReviews_ReturnsOk()
    {
        _review.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ReviewDto>());
        var c = CreateController();

        var result = await c.GetAllReviews(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateReviewStatus_ReturnsFail_WhenInvalidStatus()
    {
        var c = CreateController();

        var result = await c.UpdateReviewStatus(1, new UpdateReviewStatusRequest { Status = "invalid" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(400, api.Status);
    }

    [Fact]
    public async Task UpdateReviewStatus_ReturnsFail_WhenNotFound()
    {
        _review.Setup(r => r.UpdateStatusAsync(99, ReviewStatus.Visible)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.UpdateReviewStatus(99, new UpdateReviewStatusRequest { Status = "显示" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(404, api.Status);
    }
}
