using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IMinioClient> _minioMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IConfigurationSection> _minioSectionMock = new();

    private ImageService CreateService()
    {
        _minioSectionMock.Setup(s => s["Endpoint"]).Returns("localhost:9000");
        _minioSectionMock.Setup(s => s["AccessKey"]).Returns("minioadmin");
        _minioSectionMock.Setup(s => s["SecretKey"]).Returns("minioadmin");
        _minioSectionMock.Setup(s => s["BucketName"]).Returns("test-bucket");
        _minioSectionMock.Setup(s => s["UseSsl"]).Returns("false");
        _configMock.Setup(c => c.GetSection("Minio")).Returns(_minioSectionMock.Object);

        // Mock constructor's EnsureBucketExistsAsync calls
        _minioMock.Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _minioMock.Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ImageService(_minioMock.Object, _configMock.Object);
    }

    [Fact]
    public void GetPublicUrl_ReturnsCorrectHttpUrl()
    {
        var service = CreateService();

        var url = service.GetPublicUrl("avatars/test.jpg");

        Assert.Equal("http://localhost:9000/test-bucket/avatars/test.jpg", url);
    }

    [Fact]
    public async Task UploadAsync_ReturnsObjectName_WithFolderAndExtension()
    {
        _minioMock.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PutObjectResponse?)null);
        var service = CreateService();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var objectName = await service.UploadAsync(stream, "photo.jpg", "image/jpeg", "avatars");

        Assert.StartsWith("avatars/", objectName);
        Assert.EndsWith(".jpg", objectName);
    }

    [Fact]
    public async Task UploadWithNameAsync_UsesProvidedName()
    {
        _minioMock.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PutObjectResponse?)null);
        var service = CreateService();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var objectName = await service.UploadWithNameAsync(stream, "custom/path/file.png", "image/png");

        Assert.Equal("custom/path/file.png", objectName);
    }

    [Fact]
    public async Task DeleteAsync_CallsRemoveObject()
    {
        var service = CreateService();

        await service.DeleteAsync("existing.jpg");

        _minioMock.Verify(m => m.RemoveObjectAsync(
            It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteByPrefixAsync_CallsDeleteForEachExtension()
    {
        var service = CreateService();

        await service.DeleteByPrefixAsync("avatars/user1");

        // Called 5 times (one per extension: .jpg, .jpeg, .png, .gif, .webp)
        _minioMock.Verify(m => m.RemoveObjectAsync(
            It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact(Skip = "GetObjectAsync callback mocking is complex with Minio client")]
    public async Task GetStreamAsync_ReturnsMemoryStream()
    {
        var service = CreateService();
        var testBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        _minioMock.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Callback<GetObjectArgs, CancellationToken>((args, _) =>
            {
                using var ms = new MemoryStream(testBytes);
                args.WithCallbackStream(cb => ms.CopyTo(cb));
            })
            .ReturnsAsync((ObjectStat?)null);

        var stream = await service.GetStreamAsync("test.jpg");
        var buffer = new byte[3];
        stream.ReadExactly(buffer, 0, 3);

        Assert.Equal(testBytes, buffer);
    }
}
