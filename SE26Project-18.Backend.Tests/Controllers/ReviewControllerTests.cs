using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class ReviewControllerTests
{
    private readonly Mock<IReviewService> _reviewMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IChatService> _chatMock = new();

    private ReviewController CreateController(long userId = 1)
    {
        var c = new ReviewController(_reviewMock.Object, _notifMock.Object, _userMock.Object, _chatMock.Object);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return c;
    }

    [Fact]
    public async Task GetReviewsByUser_ReturnsOk()
    {
        _reviewMock.Setup(r => r.GetReviewsForUserAsync(1)).ReturnsAsync(new List<ReviewDto>());
        var c = CreateController();

        var result = await c.GetReviewsByUser(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task HasReviewed_ReturnsTrue()
    {
        _reviewMock.Setup(r => r.HasReviewedAsync(1, 2)).ReturnsAsync(true);
        var c = CreateController();

        var result = await c.HasReviewed(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(apiResp.Data);
    }

    [Fact]
    public async Task CreateReview_ReturnsOk_WhenSuccessful()
    {
        _reviewMock.Setup(r => r.CreateAsync(1, 2, "Great!")).ReturnsAsync(new Review { });
        _userMock.Setup(u => u.GetUserByIdAsync(1)).ReturnsAsync(new UserDto { Id = 1, Nickname = "Me" });
        var c = CreateController();

        var result = await c.CreateReview(new CreateReviewDto { RevieweeId = 2, Content = "Great!" });

        Assert.IsType<OkObjectResult>(result.Result);
        _notifMock.Verify(n => n.CreateAsync(2, "收到新评价", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateReview_ReturnsApiFail_WhenUnauthenticated()
    {
        var c = CreateController(0);
        var result = await c.CreateReview(new CreateReviewDto { RevieweeId = 2, Content = "x" });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(401, Assert.IsType<ApiResponse<bool>>(ok.Value).Status);
    }

    [Fact]
    public async Task CreateReview_ReturnsApiFail_WhenSelfReview()
    {
        _reviewMock.Setup(r => r.CreateAsync(1, 1, "x")).ThrowsAsync(new ArgumentException("不能评价自己"));
        var c = CreateController();

        var result = await c.CreateReview(new CreateReviewDto { RevieweeId = 1, Content = "x" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(400, Assert.IsType<ApiResponse<bool>>(ok.Value).Status);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsApiFail_WhenInvalidStatus()
    {
        var c = CreateController();

        var result = await c.UpdateStatus(1, new UpdateReviewStatusDto { Status = "invalid" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(400, apiResp.Status);
    }

    [Fact]
    public async Task CreateReview_ReturnsApiFail_WhenChatRestricted()
    {
        _chatMock.Setup(c => c.GetChatByUsersAsync(It.IsAny<long[]>()))
            .ReturnsAsync(new ChatDto { Id = 1, ChatStatus = "限制" });
        var c = CreateController();

        var result = await c.CreateReview(new CreateReviewDto { RevieweeId = 2, Content = "Good" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(400, Assert.IsType<ApiResponse<bool>>(ok.Value).Status);
    }
}
