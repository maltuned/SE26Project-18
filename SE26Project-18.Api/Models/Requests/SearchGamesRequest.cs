using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record SearchGamesRequest(
    [StringLength(100)] string? Query = null,
    IReadOnlyCollection<long>? TagIds = null
);
