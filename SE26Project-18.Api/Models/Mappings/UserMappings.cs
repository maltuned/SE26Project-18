using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

public static class UserMappings
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
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Signature = user.Signature,
            Gender = user.Gender,
            Status = user.Status,
            Tags = user.Tags.Select(t => t.ToResponse()).ToList(),
        };
    }
}
