using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace SE26Project_18.Api.Data;

internal static class MariaDbCompatibility
{
    private static readonly Regex MariaDbVersionPattern = new(
        @"(?<version>\d+\.\d+\.\d+)-MariaDB(?:-|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    public static ServerVersion ParseAndValidate(string versionText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionText);

        var match = MariaDbVersionPattern.Match(versionText);
        if (!match.Success || !Version.TryParse(match.Groups["version"].Value, out var version))
        {
            throw new InvalidOperationException(
                $"MariaDB 10.11.x is required, but server version '{versionText}' was detected."
            );
        }

        var serverVersion = new MariaDbServerVersion(version);
        Validate(serverVersion);
        return serverVersion;
    }

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
