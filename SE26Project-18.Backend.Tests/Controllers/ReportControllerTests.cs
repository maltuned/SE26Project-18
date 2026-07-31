using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class ReportControllerTests
{
    private readonly Mock<IReportService> _reportMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<IRecruitmentService> _recruitMock = new();
    private readonly Mock<IUserService> _userMock = new();
    private readonly Mock<IChatService> _chatMock = new();
    private readonly Mock<IReviewService> _reviewMock = new();

    private ReportController CreateController(long userId = 1)
    {
        var c = new ReportController(_reportMock.Object, _notifMock.Object, _recruitMock.Object,
            _userMock.Object, _chatMock.Object, _reviewMock.Object);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return c;
    }

    [Fact]
    public async Task SubmitReport_ReturnsOk_WhenValid()
    {
        var c = CreateController();

        var result = await c.SubmitReport(new ReportDto
        {
            TargetType = "用户", TargetId = 2, ViolationType = "谩骂", Content = "Bad behavior"
        });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitReport_ReturnsApiFail_WhenInvalidTargetType()
    {
        var c = CreateController();

        var result = await c.SubmitReport(new ReportDto
        {
            TargetType = "invalid", TargetId = 2, ViolationType = "谩骂", Content = "test"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(400, apiResp.Status);
    }
}
