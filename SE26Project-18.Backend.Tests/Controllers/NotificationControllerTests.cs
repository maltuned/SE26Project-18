using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> _notifMock = new();

    private NotificationController CreateController(long userId = 1)
    {
        var c = new NotificationController(_notifMock.Object);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        c.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
        return c;
    }

    [Fact]
    public async Task GetMyNotifications_ReturnsOk()
    {
        _notifMock.Setup(n => n.GetByUserIdAsync(1)).ReturnsAsync(new List<NotificationDto>());
        var c = CreateController();

        var result = await c.GetMyNotifications();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsOk()
    {
        _notifMock.Setup(n => n.GetUnreadCountAsync(1)).ReturnsAsync(5);
        var c = CreateController();

        var result = await c.GetUnreadCount();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsApiFail_WhenNotExists()
    {
        _notifMock.Setup(n => n.MarkAsReadAsync(99, 1)).ReturnsAsync(false);
        var c = CreateController();

        var result = await c.MarkAsRead(99);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.Equal(404, apiResp.Status);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOk()
    {
        var c = CreateController();

        var result = await c.MarkAllAsRead();

        Assert.IsType<OkObjectResult>(result.Result);
        _notifMock.Verify(n => n.MarkAllAsReadAsync(1), Times.Once);
    }
}
