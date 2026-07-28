using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Tests.Data;

public sealed class RelationshipModelTests
{
    [Fact]
    public async Task Response_HasSingleRecruitmentForeignKey()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

        var responseType = db.Model.FindEntityType(typeof(Response))!;
        var recruitmentForeignKeys = responseType
            .GetForeignKeys()
            .Where(key => key.PrincipalEntityType.ClrType == typeof(Recruitment))
            .ToList();

        var foreignKey = Assert.Single(recruitmentForeignKeys);
        Assert.Equal(nameof(Response.RecruitmentId), Assert.Single(foreignKey.Properties).Name);
    }
}
