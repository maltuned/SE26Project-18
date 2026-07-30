using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace SE26Project_18.Backend.Services;

public class ImageService : IImageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly bool _useSsl;

    public ImageService(IMinioClient client, IConfiguration configuration)
    {
        var minioConfig = configuration.GetSection("Minio");
        _endpoint = minioConfig["Endpoint"]!;
        _bucketName = minioConfig["BucketName"]!;
        _useSsl = bool.Parse(minioConfig["UseSsl"] ?? "false");

        _client = client;

        Task.Run(async () => await EnsureBucketExistsAsync()).GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!found)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                Console.WriteLine($"[MinIO] Bucket '{_bucketName}' created.");
            }

            var policy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Effect": "Allow",
                        "Principal": { "AWS": ["*"] },
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::{{_bucketName}}/*"]
                    }
                ]
            }
            """;
            await _client.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy));
            Console.WriteLine($"[MinIO] Bucket '{_bucketName}' policy set to public-read.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MinIO] Warning: Failed to ensure bucket '{_bucketName}': {ex.Message}");
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var extension = Path.GetExtension(fileName);
        var objectName = $"{folder}/{Guid.NewGuid()}{extension}";

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(putArgs);

        return objectName;
    }

    public async Task<string> UploadWithNameAsync(Stream fileStream, string objectName, string contentType)
    {
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(putArgs);

        return objectName;
    }

    public async Task<Stream> GetStreamAsync(string objectName)
    {
        var memoryStream = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
            });

        await _client.GetObjectAsync(args);
        return memoryStream;
    }

    public async Task DeleteAsync(string objectName)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);
            await _client.RemoveObjectAsync(removeArgs);
        }
        catch (ObjectNotFoundException)
        {
            // 文件不存在，忽略
        }
    }

    public async Task DeleteByPrefixAsync(string prefix)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        foreach (var ext in extensions)
        {
            await DeleteAsync($"{prefix}{ext}");
        }
    }

    public string GetPublicUrl(string objectName)
    {
        var protocol = _useSsl ? "https" : "http";
        return $"{protocol}://{_endpoint}/{_bucketName}/{objectName}";
    }
}