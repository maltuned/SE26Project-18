using Microsoft.Net.Http.Headers;

namespace SE26Project_18.Api.Infrastructure.Media;

public enum GameMediaKind
{
    Icon,
    Cover,
}

public sealed record MediaFile(
    Stream Stream,
    DateTimeOffset LastModified,
    EntityTagHeaderValue EntityTag
);

public interface IMediaService
{
    Task<MediaFile?> OpenUserAvatarAsync(long userId, CancellationToken ct);

    Task StoreUserAvatarAsync(long userId, IFormFile file, CancellationToken ct);

    Task DeleteUserAvatarAsync(long userId, CancellationToken ct);

    Task<MediaFile?> OpenGameMediaAsync(long gameId, GameMediaKind kind, CancellationToken ct);

    Task StoreGameMediaAsync(
        long gameId,
        GameMediaKind kind,
        IFormFile file,
        CancellationToken ct
    );

    Task DeleteGameMediaAsync(long gameId, GameMediaKind kind, CancellationToken ct);
}
