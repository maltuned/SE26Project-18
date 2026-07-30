using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SE26Project_18.Api.Infrastructure.Media;

internal sealed class MediaService : IMediaService
{
    private readonly AppDbContext _db;

    private readonly MediaStorageOptions _options;

    private readonly string _rootPath;

    public MediaService(
        AppDbContext db,
        IOptions<MediaStorageOptions> options,
        IWebHostEnvironment environment
    )
    {
        _db = db;
        _options = options.Value;
        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(_options.RootPath)
                ? _options.RootPath
                : Path.Combine(environment.ContentRootPath, _options.RootPath)
        );
    }

    public Task<MediaFile?> OpenUserAvatarAsync(long userId, CancellationToken ct)
    {
        return OpenAsync(GetUserAvatarPath(userId), ct);
    }

    public async Task StoreUserAvatarAsync(long userId, IFormFile file, CancellationToken ct)
    {
        if (!await _db.Users.AsNoTracking().AnyAsync(user => user.Id == userId, ct))
        {
            throw new NotFoundException("User not found.");
        }

        await StoreAsync(GetUserAvatarPath(userId), file, 512, 512, ct);
    }

    public async Task DeleteUserAvatarAsync(long userId, CancellationToken ct)
    {
        if (!await _db.Users.AsNoTracking().AnyAsync(user => user.Id == userId, ct))
        {
            throw new NotFoundException("User not found.");
        }

        File.Delete(GetUserAvatarPath(userId));
    }

    public Task<MediaFile?> OpenGameMediaAsync(
        long gameId,
        GameMediaKind kind,
        CancellationToken ct
    )
    {
        return OpenAsync(GetGameMediaPath(gameId, kind), ct);
    }

    public async Task StoreGameMediaAsync(
        long gameId,
        GameMediaKind kind,
        IFormFile file,
        CancellationToken ct
    )
    {
        await EnsureGameExistsAsync(gameId, ct);
        var (width, height) = kind == GameMediaKind.Icon ? (512, 512) : (1200, 675);
        await StoreAsync(GetGameMediaPath(gameId, kind), file, width, height, ct);
    }

    public async Task DeleteGameMediaAsync(
        long gameId,
        GameMediaKind kind,
        CancellationToken ct
    )
    {
        await EnsureGameExistsAsync(gameId, ct);
        File.Delete(GetGameMediaPath(gameId, kind));
    }

    private async Task EnsureGameExistsAsync(long gameId, CancellationToken ct)
    {
        if (!await _db.Games.AsNoTracking().AnyAsync(game => game.Id == gameId, ct))
        {
            throw new NotFoundException("Game not found.");
        }
    }

    private async Task StoreAsync(
        string destinationPath,
        IFormFile file,
        int width,
        int height,
        CancellationToken ct
    )
    {
        if (file.Length <= 0 || file.Length > _options.MaxUploadBytes)
        {
            throw new ValidationException(
                $"Image must be between 1 and {_options.MaxUploadBytes} bytes."
            );
        }

        await using var source = file.OpenReadStream();
        using var input = new MemoryStream((int)file.Length);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, ct);
            if (bytesRead == 0)
            {
                break;
            }

            if (input.Length + bytesRead > _options.MaxUploadBytes)
            {
                throw new ValidationException(
                    $"Image must be between 1 and {_options.MaxUploadBytes} bytes."
                );
            }

            await input.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
        }

        input.Position = 0;
        ImageInfo imageInfo;
        try
        {
            imageInfo =
                await Image.IdentifyAsync(input, ct)
                ?? throw new ValidationException("The uploaded file is not a valid image.");
        }
        catch (UnknownImageFormatException)
        {
            throw new ValidationException("Only JPEG, PNG, and WebP images are supported.");
        }
        catch (InvalidImageContentException)
        {
            throw new ValidationException("The uploaded image is invalid or corrupt.");
        }

        var formatName = imageInfo.Metadata.DecodedImageFormat?.Name;
        if (
            !new[] { "JPEG", "PNG", "WEBP" }.Contains(
                formatName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            throw new ValidationException("Only JPEG, PNG, and WebP images are supported.");
        }

        if ((long)imageInfo.Width * imageInfo.Height > _options.MaxPixels)
        {
            throw new ValidationException($"Image dimensions exceed {_options.MaxPixels} pixels.");
        }

        input.Position = 0;
        using var image = await LoadImageAsync(input, ct);
        image.Mutate(context =>
            context.AutoOrient().Resize(
                new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                }
            )
        );
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        var directory = Path.GetDirectoryName(destinationPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await image.SaveAsWebpAsync(
                temporaryPath,
                new WebpEncoder { Quality = 85 },
                ct
            );
            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static async Task<Image<Rgba32>> LoadImageAsync(Stream input, CancellationToken ct)
    {
        try
        {
            return await Image.LoadAsync<Rgba32>(
                new DecoderOptions { MaxFrames = 1 },
                input,
                ct
            );
        }
        catch (UnknownImageFormatException)
        {
            throw new ValidationException("Only JPEG, PNG, and WebP images are supported.");
        }
        catch (InvalidImageContentException)
        {
            throw new ValidationException("The uploaded image is invalid or corrupt.");
        }
    }

    private static async Task<MediaFile?> OpenAsync(string path, CancellationToken ct)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }

        try
        {
            var hash = await SHA256.HashDataAsync(stream, ct);
            stream.Position = 0;
            var lastModified = File.GetLastWriteTimeUtc(path);
            return new MediaFile(
                stream,
                new DateTimeOffset(lastModified, TimeSpan.Zero),
                new EntityTagHeaderValue($"\"{Convert.ToHexString(hash)}\"")
            );
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    private string GetUserAvatarPath(long userId)
    {
        return Path.Combine(_rootPath, "users", userId.ToString(), "avatar.webp");
    }

    private string GetGameMediaPath(long gameId, GameMediaKind kind)
    {
        var fileName = kind == GameMediaKind.Icon ? "icon.webp" : "cover.webp";
        return Path.Combine(_rootPath, "games", gameId.ToString(), fileName);
    }
}
