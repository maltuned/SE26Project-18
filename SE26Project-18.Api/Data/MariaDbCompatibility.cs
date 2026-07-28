using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace SE26Project_18.Api.Data;

internal static class MariaDbCompatibility
{
    public static void Validate(ServerVersion serverVersion)
    {
        ArgumentNullException.ThrowIfNull(serverVersion);

        if (serverVersion is not MariaDbServerVersion)
        {
            throw new InvalidOperationException(
                $"MariaDB 10.11.x is required, but {serverVersion} was detected."
            );
        }

        if (serverVersion.Version is not { Major: 10, Minor: 11 })
        {
            throw new InvalidOperationException(
                $"MariaDB 10.11.x is required, but version {serverVersion.Version} was detected."
            );
        }
    }
}
