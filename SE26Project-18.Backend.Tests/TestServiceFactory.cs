using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Models.Recommendations;
using SE26Project_18.Backend.Services;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Tests;

internal static class TestServiceFactory
{
    private static readonly IEmbeddingSyncScheduler EmbeddingSync = new NoOpEmbeddingSyncScheduler();

    public static GameService CreateGameService(AppDbContext db, MapperService mapper) =>
        new(db, mapper, EmbeddingSync);

    public static TagService CreateTagService(AppDbContext db, MapperService mapper) =>
        new(db, mapper, EmbeddingSync);

    public static ResponseService CreateResponseService(AppDbContext db, MapperService mapper) =>
        new(db, mapper, EmbeddingSync);

    public static RecruitmentService CreateRecruitmentService(AppDbContext db, MapperService mapper) =>
        new(
            db,
            mapper,
            new PassthroughRecommendationAlgorithm(),
            EmbeddingSync,
            new HttpContextAccessor(),
            NullLogger<RecruitmentService>.Instance);

    private sealed class NoOpEmbeddingSyncScheduler : IEmbeddingSyncScheduler
    {
        public void Schedule(EmbeddingTarget target, long entityId)
        {
        }

        public void Schedule(EmbeddingTarget target, IEnumerable<long> entityIds)
        {
        }
    }

    private sealed class PassthroughRecommendationAlgorithm : IRecruitmentRecommendationAlgorithm
    {
        public Task<IReadOnlyList<long>> RankAsync(
            long userId,
            IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<long>>(candidates.Select(candidate => candidate.Id).ToList());
    }
}
