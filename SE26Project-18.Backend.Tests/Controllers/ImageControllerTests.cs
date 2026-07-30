using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SE26Project_18.Backend.Controllers;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Controllers;

public class ImageControllerTests
{
    private readonly Mock<IImageService> _img = new();

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenInvalidExtension()
    {
        var file = CreateMockFile("test.txt");
        var c = new ImageController(_img.Object);

        var result = await c.Upload(file, "general", null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileTooLarge()
    {
        var file = CreateMockFile("test.jpg", 6 * 1024 * 1024);
        var c = new ImageController(_img.Object);

        var result = await c.Upload(file, "general", null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Upload_ReturnsOk_WhenValid()
    {
        var file = CreateMockFile("photo.jpg");
        _img.Setup(i => i.UploadAsync(It.IsAny<Stream>(), "photo.jpg", "image/jpeg", "general"))
            .ReturnsAsync("general/abc.jpg");
        var c = new ImageController(_img.Object);

        var result = await c.Upload(file, "general", null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequest_WhenInvalidUserId()
    {
        var file = CreateMockFile("avatar.jpg");
        var c = new ImageController(_img.Object);

        var result = await c.UploadAvatar(file, 0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetFile_ReturnsFile()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _img.Setup(i => i.GetStreamAsync("avatars/user1.jpg")).ReturnsAsync(stream);
        var c = new ImageController(_img.Object);

        var result = await c.GetFile("avatars/user1.jpg");

        Assert.IsType<FileStreamResult>(result);
    }

    private static IFormFile CreateMockFile(string fileName, long length = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.ContentType).Returns("image/jpeg");
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[length]));
        return mock.Object;
    }
}
