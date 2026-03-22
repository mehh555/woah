using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woah.Api.Exceptions;

namespace Woah.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            NotFoundException ex
                => (StatusCodes.Status404NotFound, "Not Found", ex.Message),

            BadRequestException ex
                => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),

            ForbiddenException ex
                => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),

            DbUpdateConcurrencyException
                => (StatusCodes.Status409Conflict, "Conflict", "The resource was modified by another request. Please retry."),

            DbUpdateException { InnerException: PostgresException pg } when pg.SqlState == "23505"
                => (StatusCodes.Status409Conflict, "Conflict", "A record with these values already exists."),

            DbUpdateException { InnerException: PostgresException pg } when pg.SqlState == "23503"
                => (StatusCodes.Status409Conflict, "Conflict", "Referenced resource does not exist or was deleted."),

            _
                => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
}