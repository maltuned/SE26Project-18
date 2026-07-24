namespace SE26Project_18.Api.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class AuthenticationException(string message)
    : ApiException(message, StatusCodes.Status401Unauthorized);

public sealed class ValidationException(string message)
    : ApiException(message, StatusCodes.Status400BadRequest);

public sealed class ForbiddenException(string message)
    : ApiException(message, StatusCodes.Status403Forbidden);

public sealed class NotFoundException(string message)
    : ApiException(message, StatusCodes.Status404NotFound);

public sealed class ConflictException(string message)
    : ApiException(message, StatusCodes.Status409Conflict);
