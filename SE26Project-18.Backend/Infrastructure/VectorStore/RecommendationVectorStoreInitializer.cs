using SE26Project_18.Backend.Repositories;

namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal sealed class RecommendationVectorStoreInitializer : IHostedService
{
    private readonly RecommendationVectorRepository _repository;

    private readonly ILogger<RecommendationVectorStoreInitializer> _logger;
    private readonly RecommendationVectorStoreReadiness _readiness;

    public RecommendationVectorStoreInitializer(
        RecommendationVectorRepository repository,
        RecommendationVectorStoreReadiness readiness,
        ILogger<RecommendationVectorStoreInitializer> logger
    )
    {
        _repository = repository;
        _readiness = readiness;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Initializing recommendation vector indexes");
            await _repository.EnsureIndexesAsync(ct);
            _readiness.MarkReady();
            _logger.LogInformation("Recommendation vector indexes are ready");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to initialize recommendation vector indexes");
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
