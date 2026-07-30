using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class UserMappings
{
    public static void ApplyTo(
        this UpdateUserRequest request,
        User user,
        IReadOnlyCollection<UserTag>? tags
    )
    {
        if (request.Nickname is not null)
        {
            user.Nickname = request.Nickname;
        }

        if (request.Signature is not null)
        {
            user.Signature = request.Signature;
        }

        if (request.Gender.HasValue)
        {
            user.Gender = request.Gender.Value;
        }

        if (tags is not null)
        {
            user.Tags = tags.ToList();
        }
    }

    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse(
            user.Id,
            user.Username,
            user.Nickname,
            user.Signature,
            user.Gender,
            user.Status,
            user.Role == UserRole.Admin,
            user.Tags.Select(t => t.ToResponse()).ToList(),
            $"/api/v1/users/{user.Id}/avatar"
        );
    }
}
