using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SE26Project_18.Api.Data;

internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalMigrationConnection =
        "Server=127.0.0.1;Port=3306;Database=se26project_18_migration_generation_only;User=migration_generation_only;Password=not-a-runtime-credential;Connection Timeout=1;";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? LocalMigrationConnection;
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContextConfiguration.Configure(options, connectionString);

        return new AppDbContext(options.Options);
    }
}
