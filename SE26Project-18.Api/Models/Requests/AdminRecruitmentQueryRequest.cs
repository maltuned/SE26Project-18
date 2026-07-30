using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Infrastructure.Pagination;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Requests;

public sealed record AdminRecruitmentQueryRequest(
    string? Query,
    [Range(1, long.MaxValue)] long? RecruiterId,
    [Range(1, long.MaxValue)] long? GameId,
    RecruitmentStatus? Status,
    [Range(1, OffsetPagination.MaxPage)] int Page = 1,
    [Range(1, 100)] int PageSize = 20
);
