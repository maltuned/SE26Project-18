using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Requests;

public sealed record UpdateUserRequest(
    [StringLength(100)] string? Nickname = null,
    [StringLength(500)] string? Signature = null,
    Gender? Gender = null,
    IReadOnlyCollection<long>? TagIds = null
);
