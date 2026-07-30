using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Models.Requests;

namespace SE26Project_18.Api.Infrastructure.Authentication;

internal sealed class AdminBootstrapOptionsValidator : IValidateOptions<AdminBootstrapOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminBootstrapOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var constructor = typeof(RegisterRequest).GetConstructors().Single();
        var parameters = constructor.GetParameters();
        var values = new object?[] { options.Username, options.Password };
        var failures = new List<string>();

        for (var index = 0; index < parameters.Length; index++)
        {
            var context = new ValidationContext(options)
            {
                MemberName = parameters[index].Name,
            };
            var results = new List<ValidationResult>();
            if (
                !Validator.TryValidateValue(
                    values[index],
                    context,
                    results,
                    parameters[index].GetCustomAttributes<ValidationAttribute>()
                )
            )
            {
                failures.AddRange(
                    results.Select(result =>
                        result.ErrorMessage ?? "Invalid admin bootstrap credentials."
                    )
                );
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
