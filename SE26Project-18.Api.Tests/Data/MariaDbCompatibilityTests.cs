using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SE26Project_18.Api.Data;

namespace SE26Project_18.Api.Tests.Data;

public sealed class MariaDbCompatibilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    public void Validate_AcceptsMariaDb1011PatchVersions(int patch)
    {
        MariaDbCompatibility.Validate(
            new MariaDbServerVersion(new Version(10, 11, patch))
        );
    }

    [Fact]
    public void Validate_RejectsMySql()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MariaDbCompatibility.Validate(new MySqlServerVersion(new Version(8, 0, 36)))
        );
    }

    [Theory]
    [InlineData(10, 6)]
    [InlineData(11, 4)]
    public void Validate_RejectsOtherMariaDbVersions(int major, int minor)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MariaDbCompatibility.Validate(
                new MariaDbServerVersion(new Version(major, minor, 1))
            )
        );
    }
}
