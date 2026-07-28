using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record CreateGameRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(4_000)] string Description,
    IReadOnlyCollection<long>? TagIds = null
);
