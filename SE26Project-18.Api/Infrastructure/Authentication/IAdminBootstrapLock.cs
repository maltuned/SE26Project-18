namespace SE26Project_18.Api.Infrastructure.Authentication;

internal interface IAdminBootstrapLock
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken ct);
}
