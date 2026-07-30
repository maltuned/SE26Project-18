namespace SE26Project_18.Api.Data;

internal static class DatabaseMigrationState
{
    public const string ManualMigrationGuidance =
        "Provision the database externally, then run 'dotnet ef database update --project SE26Project-18.Api' with DDL credentials before starting the API.";

    public static void Validate(
        IReadOnlyCollection<string> knownMigrations,
        IReadOnlyCollection<string> appliedMigrations,
        IReadOnlyCollection<string> pendingMigrations
    )
    {
        ArgumentNullException.ThrowIfNull(knownMigrations);
        ArgumentNullException.ThrowIfNull(appliedMigrations);
        ArgumentNullException.ThrowIfNull(pendingMigrations);

        if (knownMigrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"The API binary contains no database migrations. {ManualMigrationGuidance}"
            );
        }

        if (appliedMigrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"The database has no applied API migrations. {ManualMigrationGuidance}"
            );
        }

        var known = new HashSet<string>(knownMigrations, StringComparer.Ordinal);
        var unknownApplied = appliedMigrations.Where(migration => !known.Contains(migration)).ToArray();
        if (unknownApplied.Length > 0)
        {
            throw new InvalidOperationException(
                $"The database contains migrations unknown to this API binary: {string.Join(", ", unknownApplied)}. Deploy an API version that contains every applied migration; never run an older binary against a newer schema."
            );
        }

        if (pendingMigrations.Count > 0)
        {
            throw new InvalidOperationException(
                $"The database has pending API migrations: {string.Join(", ", pendingMigrations)}. {ManualMigrationGuidance}"
            );
        }
    }
}
