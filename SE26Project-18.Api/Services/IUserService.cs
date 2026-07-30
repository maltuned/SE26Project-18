using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(long id, CancellationToken ct);

    Task EnsureActiveAsync(long id, CancellationToken ct);

    Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct);

    Task<UserResponse> SetSuspensionAsync(
        long actorId,
        long id,
        SetUserSuspensionRequest request,
        CancellationToken ct
    );
}
