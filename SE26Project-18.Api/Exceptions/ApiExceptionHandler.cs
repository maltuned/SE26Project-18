using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milvus.Client;
using MySqlConnector;
using SE26Project_18.Api.Models.Exceptions;

namespace SE26Project_18.Api.Exceptions;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        var (statusCode, title, detail) = exception switch
        {
            ResponseAlreadyProcessedException domainException => (
                StatusCodes.Status409Conflict,
                "Response state conflict",
                domainException.Message
            ),
            ApiException apiException => (
                apiException.StatusCode,
                "Request failed",
                apiException.Message
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrent update",
                "The resource changed before the operation could be completed."
            ),
            DbUpdateException { InnerException: MySqlException { Number: 1062 } } => (
                StatusCodes.Status409Conflict,
                "Persistence conflict",
                "The operation conflicts with existing data."
            ),
            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "External dependency unavailable",
                "The embedding service is currently unavailable."
            ),
            MilvusException => (
                StatusCodes.Status503ServiceUnavailable,
                "Vector store unavailable",
                "The vector store is currently unavailable."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred."
            ),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(
                exception,
                "Unhandled exception while handling {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path
            );

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        if (statusCode == StatusCodes.Status401Unauthorized)
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path,
            },
            ct
        );
        return true;
    }
}
