using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Infrastructure.Embedding;

internal sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Dimension { get; init; }

    public bool SendDimensions { get; init; } = true;

    [Range(1, 2_048)]
    public int RequestBatchSize { get; init; } = 128;
}
