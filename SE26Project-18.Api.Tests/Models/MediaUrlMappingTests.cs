using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;

namespace SE26Project_18.Api.Tests.Models;

public sealed class MediaUrlMappingTests
{
    [Fact]
    public void UserResponse_IncludesRelativeAvatarUrl()
    {
        var response = new User("user", "hash", UserRole.User).ToResponse();

        Assert.Equal($"/api/v1/users/{response.Id}/avatar", response.AvatarUrl);
    }

    [Fact]
    public void GameResponse_IncludesRelativeIconAndCoverUrls()
    {
        var response = new Game("game").ToResponse();

        Assert.Equal($"/api/v1/games/{response.Id}/icon", response.IconUrl);
        Assert.Equal($"/api/v1/games/{response.Id}/cover", response.CoverUrl);
    }
}
