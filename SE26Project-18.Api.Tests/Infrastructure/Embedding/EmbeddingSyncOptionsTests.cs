using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Tests.Infrastructure.Embedding;

public sealed class EmbeddingSyncOptionsTests
{
    [Fact]
    public void Validate_RejectsPrefetchBelowBatchSize()
    {
        var options = new EmbeddingSyncOptions { BatchSize = 100, PrefetchCount = 50 };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true
        );

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains("PrefetchCount"));
    }
}
