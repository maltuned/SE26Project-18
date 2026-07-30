using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace SE26Project_18.Api.Data;

internal static class AppDbContextConfiguration
{
    public static readonly MariaDbServerVersion SupportedServerVersion =
        new(new Version(10, 11, 0));

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        options.UseMySql(connectionString, SupportedServerVersion);
    }
}
