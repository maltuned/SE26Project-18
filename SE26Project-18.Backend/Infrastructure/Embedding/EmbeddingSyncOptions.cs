using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Backend.Infrastructure.Embedding;

internal sealed class EmbeddingSyncOptions : IValidatableObject
{
    public const string SectionName = "EmbeddingSync";

    [Range(1, ushort.MaxValue)]
    public int BatchSize { get; init; } = 100;

    [Range(1, 60_000)]
    public int MaxBatchWaitMilliseconds { get; init; } = 1_000;

    [Range(1, ushort.MaxValue)]
    public int PrefetchCount { get; init; } = 200;

    [Range(1, 3_600)]
    public int ProcessingTimeoutSeconds { get; init; } = 120;

    [Range(1, 10_000)]
    public int MilvusBatchSize { get; init; } = 100;

    [Range(100, 60_000)]
    public int OutboxPollMilliseconds { get; init; } = 500;

    [Range(1, 10_000)]
    public int OutboxPublishBatchSize { get; init; } = 100;

    [Range(3, 3_600)]
    public int OutboxLeaseSeconds { get; init; } = 30;

    [Range(1, 8_760)]
    public int OutboxRetentionHours { get; init; } = 72;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PrefetchCount < BatchSize)
        {
            yield return new ValidationResult(
                "EmbeddingSync PrefetchCount must be greater than or equal to BatchSize.",
                [nameof(PrefetchCount), nameof(BatchSize)]
            );
        }
    }
}
