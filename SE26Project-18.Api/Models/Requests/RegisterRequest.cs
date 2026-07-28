using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record RegisterRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, StringLength(100, MinimumLength = 8)] string Password
);
