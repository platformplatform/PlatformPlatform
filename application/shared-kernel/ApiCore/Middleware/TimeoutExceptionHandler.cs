using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class TimeoutExceptionHandler(ILogger<TimeoutExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not TimeoutException)
        {
            // Return false to continue with the default behavior
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception, "An timeout exception occurred while processing the request. TraceId: {TraceId}.",
            traceId
        );

        await Results.Problem(
            title: "Request Timeout",
            detail: $"{httpContext.Request.Method} {httpContext.Request.Path} {httpContext.Request.QueryString}".Trim(),
            statusCode: (int)HttpStatusCode.RequestTimeout,
            extensions: new Dictionary<string, object?> { { "traceId", traceId } }
        ).ExecuteAsync(httpContext);

        // Return true to signal that this exception is handled
        return true;
    }
}