using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Infrastructure.Pagination;

namespace SE26Project_18.Api.Models.Requests;

public sealed record AdminGameQueryRequest(
    string? Query,
    [Range(1, OffsetPagination.MaxPage)] int Page = 1,
    [Range(1, 100)] int PageSize = 20
);
