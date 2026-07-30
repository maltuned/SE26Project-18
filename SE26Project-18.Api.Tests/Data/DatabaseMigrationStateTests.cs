using SE26Project_18.Api.Data;

namespace SE26Project_18.Api.Tests.Data;

public sealed class DatabaseMigrationStateTests
{
    [Fact]
    public void Validate_RejectsDatabaseWithoutAppliedMigrations()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseMigrationState.Validate(
                ["20260730000000_InitialCreate"],
                [],
                ["20260730000000_InitialCreate"]
            )
        );

        Assert.Contains(DatabaseMigrationState.ManualMigrationGuidance, exception.Message);
    }

    [Fact]
    public void Validate_RejectsPendingMigrations()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseMigrationState.Validate(
                ["20260730000000_InitialCreate", "20260731000000_AddFeature"],
                ["20260730000000_InitialCreate"],
                ["20260731000000_AddFeature"]
            )
        );

        Assert.Contains("20260731000000_AddFeature", exception.Message);
        Assert.Contains(DatabaseMigrationState.ManualMigrationGuidance, exception.Message);
    }

    [Fact]
    public void Validate_AcceptsAppliedCurrentSchema()
    {
        DatabaseMigrationState.Validate(
            ["20260730000000_InitialCreate"],
            ["20260730000000_InitialCreate"],
            []
        );
    }

    [Fact]
    public void Validate_RejectsMigrationUnknownToCurrentBinary()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseMigrationState.Validate(
                ["20260730000000_InitialCreate"],
                ["20260730000000_InitialCreate", "20260801000000_NewerSchema"],
                []
            )
        );

        Assert.Contains("20260801000000_NewerSchema", exception.Message);
        Assert.Contains("older binary", exception.Message);
    }

    [Fact]
    public void Validate_RejectsBinaryWithoutMigrations()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseMigrationState.Validate([], ["20260730000000_InitialCreate"], [])
        );
    }
}
