using Microsoft.Extensions.Configuration;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Recommendations;

public sealed class MinioInitializationTests
{
    [Fact]
    public async Task ImageService_InitializesBucketAndSupportsObjectLifecycle()
    {
        if (Environment.GetEnvironmentVariable("RUN_MINIO_INTEGRATION") != "1")
            return;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Minio:Endpoint"] = "localhost:9000",
                ["Minio:AccessKey"] = "minioadmin",
                ["Minio:SecretKey"] = "minioadmin",
                ["Minio:BucketName"] = "game-assets",
                ["Minio:UseSsl"] = "false",
            })
            .Build();
        var service = new ImageService(configuration);
        const string objectName = "integration/minio-initialization-test.jpg";
        await using var upload = new MemoryStream([1, 2, 3, 4]);

        await service.UploadWithNameAsync(upload, objectName, "image/jpeg");
        await using var downloaded = await service.GetStreamAsync(objectName);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ((MemoryStream)downloaded).ToArray());
        await service.DeleteAsync(objectName);
    }
}
