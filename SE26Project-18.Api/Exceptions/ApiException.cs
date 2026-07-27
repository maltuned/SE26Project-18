namespace SE26Project_18.Api.Exceptions;

internal abstract class ApiException : Exception
{
    protected ApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

internal sealed class AuthenticationException(string message)
    : ApiException(message, StatusCodes.Status401Unauthorized);

internal sealed class ValidationException(string message)
    : ApiException(message, StatusCodes.Status400BadRequest);

internal sealed class ForbiddenException(string message)
    : ApiException(message, StatusCodes.Status403Forbidden);

internal sealed class NotFoundException(string message)
    : ApiException(message, StatusCodes.Status404NotFound);

internal sealed class ConflictException(string message)
    : ApiException(message, StatusCodes.Status409Conflict);
