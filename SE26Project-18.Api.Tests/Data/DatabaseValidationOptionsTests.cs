using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Data;

namespace SE26Project_18.Api.Tests.Data;

public sealed class DatabaseValidationOptionsTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        Assert.Empty(Validate(new DatabaseValidationOptions()));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(11, 2)]
    [InlineData(3, -1)]
    [InlineData(3, 61)]
    public void OutOfRangeValues_AreInvalid(int maxRetryAttempts, int retryDelaySeconds)
    {
        var options = new DatabaseValidationOptions
        {
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelaySeconds = retryDelaySeconds,
        };

        Assert.NotEmpty(Validate(options));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(
        DatabaseValidationOptions options
    )
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, true);
        return results;
    }
}
