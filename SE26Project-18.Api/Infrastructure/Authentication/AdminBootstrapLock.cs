using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;

namespace SE26Project_18.Api.Infrastructure.Authentication;

internal sealed class AdminBootstrapLock : IAdminBootstrapLock
{
    private const string LockName = "SE26Project-18:first-admin-bootstrap";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InMemoryLocks = new();

    private readonly AppDbContext _db;

    public AdminBootstrapLock(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct)
    {
        if (!_db.Database.IsRelational())
        {
            var provider = _db.Database.ProviderName ?? "InMemory";
            var semaphore = InMemoryLocks.GetOrAdd(provider, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(ct);
            return new SemaphoreReleaser(semaphore);
        }

        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT GET_LOCK(@name, 60);";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = LockName;
            command.Parameters.Add(parameter);
            var result = await command.ExecuteScalarAsync(ct);
            if (Convert.ToInt32(result) != 1)
            {
                throw new InvalidOperationException("Timed out acquiring the admin bootstrap lock.");
            }

            return new DatabaseReleaser(connection, shouldClose);
        }
        catch
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }

            throw;
        }
    }

    private sealed class DatabaseReleaser(
        System.Data.Common.DbConnection connection,
        bool shouldClose
    ) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT RELEASE_LOCK(@name);";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@name";
                parameter.Value = LockName;
                command.Parameters.Add(parameter);
                await command.ExecuteScalarAsync(CancellationToken.None);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }

    private sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
