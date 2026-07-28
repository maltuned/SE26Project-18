using SE26Project_18.Api.Models.Recommendations;

namespace SE26Project_18.Api.Services.Recommendations;

internal interface IUserPreferenceProfileBuilder
{
    Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct);
}
