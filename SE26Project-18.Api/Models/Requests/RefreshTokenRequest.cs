using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record RefreshTokenRequest([Required] string RefreshToken);
