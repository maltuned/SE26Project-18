using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SE26Project_18.Api.Controllers;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Tests.Controllers;

public sealed class AdminControllerTests
{
    [Fact]
    public void Controller_RequiresAdminPolicyAndUsesAdminRoute()
    {
        var authorize = Assert.Single(
            typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
        ) as AuthorizeAttribute;
        var route = Assert.Single(
            typeof(AdminController).GetCustomAttributes(typeof(RouteAttribute), true)
        ) as RouteAttribute;

        Assert.Equal("RequireAdmin", authorize!.Policy);
        Assert.Equal("api/v1/admin", route!.Template);
    }

    [Theory]
    [MemberData(nameof(InvalidPageRequests))]
    public void QueryModels_RejectPageSizesOverOneHundred(object request)
    {
        AssertConstructorParameterIsInvalid(request, "PageSize");
    }

    [Theory]
    [MemberData(nameof(OverflowingPageRequests))]
    public void QueryModels_RejectPagesThatCouldOverflowOffset(object request)
    {
        AssertConstructorParameterIsInvalid(request, "Page");
    }

    [Fact]
    public async Task GetUsers_ReturnsServicePage()
    {
        var expected = new PagedResponse<UserResponse>([], 1, 20, 0, 0);
        var service = new StubAdminService { Users = expected };
        var controller = new AdminController(service);

        var result = await controller.GetUsers(
            new AdminUserQueryRequest(null, null, null),
            CancellationToken.None
        );

        Assert.Same(expected, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    public static TheoryData<object> InvalidPageRequests =>
        new()
        {
            new AdminUserQueryRequest(null, null, null, 1, 101),
            new AdminGameQueryRequest(null, 1, 101),
            new AdminRecruitmentQueryRequest(null, null, null, null, 1, 101),
        };

    public static TheoryData<object> OverflowingPageRequests =>
        new()
        {
            new AdminUserQueryRequest(null, null, null, int.MaxValue, 100),
            new AdminGameQueryRequest(null, int.MaxValue, 100),
            new AdminRecruitmentQueryRequest(null, null, null, null, int.MaxValue, 100),
        };

    private static void AssertConstructorParameterIsInvalid(object request, string memberName)
    {
        var constructor = Assert.Single(request.GetType().GetConstructors());
        var parameter = Assert.Single(
            constructor.GetParameters(),
            candidate => candidate.Name == memberName
        );
        var value = request.GetType().GetProperty(memberName)!.GetValue(request);
        var attributes = parameter.GetCustomAttributes(typeof(ValidationAttribute), true);

        Assert.NotEmpty(attributes);
        Assert.Contains(attributes, attribute => !((ValidationAttribute)attribute).IsValid(value));
    }

    private sealed class StubAdminService : IAdminService
    {
        public required PagedResponse<UserResponse> Users { get; init; }

        public Task<PagedResponse<UserResponse>> GetUsersAsync(
            AdminUserQueryRequest request,
            CancellationToken ct
        ) => Task.FromResult(Users);

        public Task<PagedResponse<GameResponse>> GetGamesAsync(
            AdminGameQueryRequest request,
            CancellationToken ct
        ) => throw new NotSupportedException();

        public Task<PagedResponse<RecruitmentResponse>> GetRecruitmentsAsync(
            AdminRecruitmentQueryRequest request,
            CancellationToken ct
        ) => throw new NotSupportedException();
    }
}
