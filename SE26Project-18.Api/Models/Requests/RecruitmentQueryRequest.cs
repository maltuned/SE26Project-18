using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record RecruitmentQueryRequest(
    [Range(1, long.MaxValue)] long? GameId,
    IReadOnlyCollection<long>? GameTagIds,
    IReadOnlyCollection<long>? RecruitmentTagIds,
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int PageSize = 20
);
