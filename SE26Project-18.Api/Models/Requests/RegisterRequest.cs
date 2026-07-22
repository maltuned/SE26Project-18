namespace SE26Project_18.Api.Models.Requests;

public sealed class RegisterRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}
