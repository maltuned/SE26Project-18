using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Infrastructure.Media;

internal sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    [Required]
    public string RootPath { get; init; } = "media";

    [Range(1, 5 * 1024 * 1024)]
    public long MaxUploadBytes { get; init; } = 5 * 1024 * 1024;

    [Range(1, 100_000_000)]
    public long MaxPixels { get; init; } = 12_000_000;
}
