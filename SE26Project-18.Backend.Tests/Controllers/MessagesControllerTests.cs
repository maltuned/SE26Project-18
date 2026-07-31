using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _msgMock = new();

    [Fact]
    public async Task GetMessagesByChat_ReturnsOk()
    {
        _msgMock.Setup(m => m.GetMessagesByChatAsync(1)).ReturnsAsync(new List<MessageDto>());
        var c = new MessagesController(_msgMock.Object);

        var result = await c.GetMessagesByChat(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SendMessage_ReturnsApiFail_OnKeyNotFound()
    {
        _msgMock.Setup(m => m.SendMessageAsync(99, 1, 2, "hi"))
            .ThrowsAsync(new KeyNotFoundException());
        var c = new MessagesController(_msgMock.Object);

        var result = await c.SendMessage(new SendMessageRequest { ChatId = 99, SenderId = 1, ReceiverId = 2, Content = "hi" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<MessageDto>>(ok.Value);
        Assert.Equal(404, apiResp.Status);
    }

    [Fact]
    public async Task SendMessage_ReturnsApiFail_OnInvalidOperation()
    {
        _msgMock.Setup(m => m.SendMessageAsync(1, 1, 2, "hi"))
            .ThrowsAsync(new InvalidOperationException("聊天已关闭"));
        var c = new MessagesController(_msgMock.Object);

        var result = await c.SendMessage(new SendMessageRequest { ChatId = 1, SenderId = 1, ReceiverId = 2, Content = "hi" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var apiResp = Assert.IsType<ApiResponse<MessageDto>>(ok.Value);
        Assert.Equal(403, apiResp.Status);
    }
}
