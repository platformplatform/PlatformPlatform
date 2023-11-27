using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception, "An error occurred while processing the request. TraceId: {TraceId}.",
            traceId
        );

        await Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while processing the request.",
            statusCode: (int)HttpStatusCode.InternalServerError,
            extensions: new Dictionary<string, object?> { { "traceId", traceId } }
        ).ExecuteAsync(httpContext);

        // Return true to signal that this exception is handled
        return true;
    }
}