using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Authentication;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Tests.Infrastructure.Authentication;

public sealed class AccessTokenUserValidatorTests
{
    [Theory]
    [InlineData(UserStatus.Online, true)]
    [InlineData(UserStatus.Offline, true)]
    [InlineData(UserStatus.Suspended, false)]
    public async Task IsAllowedAsync_UsesCurrentDatabaseStatus(UserStatus status, bool expected)
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User) { Status = status };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())])
        );

        var result = await new AccessTokenUserValidator(db)
            .IsAllowedAsync(principal, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task IsAllowedAsync_RejectsMissingInvalidOrUnknownUserId()
    {
        await using var db = CreateDbContext();
        var validator = new AccessTokenUserValidator(db);

        Assert.False(
            await validator.IsAllowedAsync(new ClaimsPrincipal(), CancellationToken.None)
        );
        Assert.False(
            await validator.IsAllowedAsync(
                PrincipalWithId("invalid"),
                CancellationToken.None
            )
        );
        Assert.False(
            await validator.IsAllowedAsync(PrincipalWithId("42"), CancellationToken.None)
        );
    }

    private static ClaimsPrincipal PrincipalWithId(string id)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)])
        );
    }

    private static AppDbContext CreateDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
    }
}
