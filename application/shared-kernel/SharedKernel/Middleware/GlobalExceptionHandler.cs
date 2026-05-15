using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SharedKernel.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        int statusCode;
        string title;
        string detail;

        switch (exception)
        {
            case ArgumentOutOfRangeException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Bad Request";
                detail = exception.Message;
                break;
            case TimeoutException:
                statusCode = StatusCodes.Status408RequestTimeout;
                title = "Request Timeout";
                detail = $"{httpContext.Request.Method} {httpContext.Request.Path} {httpContext.Request.QueryString}".Trim();
                break;
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = "The data was modified by another process. Please try again.";
                break;
            // PG 23505 unique_violation wraps as DbUpdateException with a PostgresException inner.
            // EF Core does not auto-map this to DbUpdateConcurrencyException (which is reserved for
            // zero-row UPDATE). Treat the race as a 409 so SPA retry/error handling can act on it.
            case DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }:
                statusCode = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = "The resource was modified by another process. Please try again.";
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                detail = "An error occurred while processing the request.";
                break;
        }

        logger.LogError(
            exception,
            "A {ExceptionType} occurred while processing the request. TraceId: {TraceId}.", exception.GetType().Name, traceId
        );

        await Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { { "traceId", traceId } }
        ).ExecuteAsync(httpContext);

        // Return true to signal that this exception is handled
        return true;
    }
}
