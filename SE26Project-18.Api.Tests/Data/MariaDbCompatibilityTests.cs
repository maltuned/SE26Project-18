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

    [Theory]
    [InlineData("10.11.8-MariaDB-0ubuntu0.24.04.1", 8)]
    [InlineData("10.11.14-MariaDB", 14)]
    [InlineData("5.5.5-10.11.11-MariaDB", 11)]
    public void ParseAndValidate_AcceptsMariaDbVersionOutput(string versionText, int patch)
    {
        var parsed = MariaDbCompatibility.ParseAndValidate(versionText);

        Assert.IsType<MariaDbServerVersion>(parsed);
        Assert.Equal(new Version(10, 11, patch), parsed.Version);
    }

    [Fact]
    public void ParseAndValidate_RejectsMySqlVersionOutput()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MariaDbCompatibility.ParseAndValidate("8.0.36-0ubuntu0.24.04.1")
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
