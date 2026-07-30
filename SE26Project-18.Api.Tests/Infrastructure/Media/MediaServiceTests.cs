using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Media;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SE26Project_18.Api.Tests.Infrastructure.Media;

public sealed class MediaServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"se26-media-tests-{Guid.NewGuid():N}"
    );

    [Fact]
    public async Task StoreUserAvatar_ProducesDeterministicSanitizedWebp()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var upload = await CreatePngFormFileAsync(800, 400);

        await service.StoreUserAvatarAsync(user.Id, upload, CancellationToken.None);

        var expectedPath = Path.Combine(
            _rootPath,
            "users",
            user.Id.ToString(),
            "avatar.webp"
        );
        Assert.True(File.Exists(expectedPath));
        var imageInfo = await Image.IdentifyAsync(expectedPath);
        Assert.NotNull(imageInfo);
        Assert.Equal(512, imageInfo.Width);
        Assert.Equal(512, imageInfo.Height);
        Assert.Equal("Webp", imageInfo.Metadata.DecodedImageFormat?.Name);
        Assert.Null(imageInfo.Metadata.ExifProfile);

        var media = await service.OpenUserAvatarAsync(user.Id, CancellationToken.None);
        Assert.NotNull(media);
        Assert.True(media.Stream.CanRead);
        Assert.StartsWith("\"", media.EntityTag.Tag.Value);
        await media.Stream.DisposeAsync();
    }

    [Fact]
    public async Task StoreGameCover_ProducesExpectedCropAndDeleteRemovesIt()
    {
        await using var db = CreateDbContext();
        var game = new Game("game");
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var upload = await CreatePngFormFileAsync(300, 600);

        await service.StoreGameMediaAsync(
            game.Id,
            GameMediaKind.Cover,
            upload,
            CancellationToken.None
        );

        var expectedPath = Path.Combine(
            _rootPath,
            "games",
            game.Id.ToString(),
            "cover.webp"
        );
        var imageInfo = await Image.IdentifyAsync(expectedPath);
        Assert.NotNull(imageInfo);
        Assert.Equal(1200, imageInfo.Width);
        Assert.Equal(675, imageInfo.Height);

        await service.DeleteGameMediaAsync(
            game.Id,
            GameMediaKind.Cover,
            CancellationToken.None
        );
        Assert.False(File.Exists(expectedPath));
    }

    [Fact]
    public async Task Store_RejectsUnsupportedOrOversizedImages()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, maxPixels: 100);
        var textUpload = CreateFormFile([1, 2, 3, 4]);
        var imageUpload = await CreatePngFormFileAsync(11, 10);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StoreUserAvatarAsync(user.Id, textUpload, CancellationToken.None)
        );
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.StoreUserAvatarAsync(user.Id, imageUpload, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GameWrites_RejectMissingEntityWithoutCreatingFiles()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var upload = await CreatePngFormFileAsync(10, 10);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.StoreGameMediaAsync(
                42,
                GameMediaKind.Icon,
                upload,
                CancellationToken.None
            )
        );

        Assert.False(Directory.Exists(_rootPath));
    }

    [Fact]
    public async Task OpenMissingMedia_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        Assert.Null(await service.OpenUserAvatarAsync(1, CancellationToken.None));
        Assert.Null(
            await service.OpenGameMediaAsync(1, GameMediaKind.Icon, CancellationToken.None)
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private MediaService CreateService(AppDbContext db, long maxPixels = 40_000_000)
    {
        return new MediaService(
            db,
            Options.Create(
                new MediaStorageOptions
                {
                    RootPath = _rootPath,
                    MaxUploadBytes = 5 * 1024 * 1024,
                    MaxPixels = maxPixels,
                }
            ),
            new TestWebHostEnvironment { ContentRootPath = _rootPath }
        );
    }

    private static async Task<FormFile> CreatePngFormFileAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.CornflowerBlue);
        var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "file", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };
    }

    private static FormFile CreateFormFile(byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", "file.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };
    }

    private static AppDbContext CreateDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;
    }
}
