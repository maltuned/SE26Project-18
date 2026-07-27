using SE26Project_18.Api.Repositories;

namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed class RecommendationVectorStoreInitializer : IHostedService
{
    private readonly RecommendationVectorRepository _repository;

    private readonly ILogger<RecommendationVectorStoreInitializer> _logger;

    public RecommendationVectorStoreInitializer(
        RecommendationVectorRepository repository,
        ILogger<RecommendationVectorStoreInitializer> logger
    )
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Initializing recommendation vector indexes");
            await _repository.EnsureIndexesAsync(ct);
            _logger.LogInformation("Recommendation vector indexes are ready");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to initialize recommendation vector indexes");
            throw;
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
