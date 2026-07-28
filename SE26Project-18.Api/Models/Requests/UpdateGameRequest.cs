using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record UpdateGameRequest(
    [StringLength(200)] string? Name = null,
    [StringLength(4_000)] string? Description = null,
    IReadOnlyCollection<long>? TagIds = null
);
