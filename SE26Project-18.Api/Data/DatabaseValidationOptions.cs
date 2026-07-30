using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Data;

internal sealed class DatabaseValidationOptions
{
    public const string SectionName = "DatabaseValidation";

    [Range(1, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(0, 60)]
    public int RetryDelaySeconds { get; init; } = 2;
}
