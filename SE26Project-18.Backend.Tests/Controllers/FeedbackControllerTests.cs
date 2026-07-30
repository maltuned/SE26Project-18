using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class FeedbackControllerTests
{
    private readonly Mock<IFeedbackService> _feedbackMock = new();
    private readonly Mock<INotificationService> _notifMock = new();

    private FeedbackController CreateController(long userId = 1)
    {
        var c = new FeedbackController(_feedbackMock.Object, _notifMock.Object);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return c;
    }

    [Fact]
    public async Task SubmitFeedback_ReturnsOk_WhenValid()
    {
        var c = CreateController();

        var result = await c.SubmitFeedback(new FeedbackDto { Type = "内容反馈", Content = "Great app" });

        Assert.IsType<OkObjectResult>(result.Result);
        _feedbackMock.Verify(f => f.SubmitFeedbackAsync(1, FeedbackType.ContentFeedback, "Great app"), Times.Once);
    }

    [Fact]
    public async Task SubmitFeedback_ReturnsApiFail_WhenInvalidType()
    {
        var c = CreateController();

        var result = await c.SubmitFeedback(new FeedbackDto { Type = "invalid", Content = "test" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(400, apiResp.Status);
    }
}
