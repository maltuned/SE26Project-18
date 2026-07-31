using SE26Project_18.Backend.Models.Recommendations;

namespace SE26Project_18.Backend.Services.Recommendations;

internal interface IUserPreferenceProfileBuilder
{
    Task<UserPreferenceProfile> BuildAsync(long userId, CancellationToken ct);
}
