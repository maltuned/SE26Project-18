using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SE26Project_18.Api.Data;

internal sealed class DatabaseStartupValidator
{
    private readonly AppDbContext _db;
    private readonly DatabaseValidationOptions _options;
    private readonly ILogger<DatabaseStartupValidator> _logger;

    public DatabaseStartupValidator(
        AppDbContext db,
        IOptions<DatabaseValidationOptions> options,
        ILogger<DatabaseStartupValidator> logger
    )
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ValidateWithRetryAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
            when (exception.Message.Contains(DatabaseMigrationState.ManualMigrationGuidance))
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Database startup validation failed. {DatabaseMigrationState.ManualMigrationGuidance}",
                exception
            );
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task ValidateWithRetryAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                await _db.Database.OpenConnectionAsync(cancellationToken);
                await ValidateServerVersionAsync(cancellationToken);

                var known = _db.Database.GetMigrations().ToArray();
                var applied = (
                    await _db.Database.GetAppliedMigrationsAsync(cancellationToken)
                ).ToArray();
                var pending = (
                    await _db.Database.GetPendingMigrationsAsync(cancellationToken)
                ).ToArray();
                DatabaseMigrationState.Validate(known, applied, pending);
                return;
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException
                    && IsRetryable(exception)
                    && attempt < _options.MaxRetryAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Database validation attempt {Attempt} of {MaxAttempts} failed.",
                    attempt,
                    _options.MaxRetryAttempts
                );
                await _db.Database.CloseConnectionAsync();
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                    cancellationToken
                );
            }
        }
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception is DbException or TimeoutException or IOException;
    }

    private async Task ValidateServerVersionAsync(CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT VERSION()";
        command.CommandType = CommandType.Text;
        var versionText = await command.ExecuteScalarAsync(cancellationToken) as string;

        if (string.IsNullOrWhiteSpace(versionText))
        {
            throw new InvalidOperationException("MariaDB did not return a server version.");
        }

        MariaDbCompatibility.ParseAndValidate(versionText);
    }
}
