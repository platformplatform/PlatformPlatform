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

        logger.LogError(exception, "An timeout exception occurred while processing the request.");

        await Results.Problem(
            title: "Request Timeout",
            detail: $"{httpContext.Request.Method} {httpContext.Request.Path} {httpContext.Request.QueryString}".Trim(),
            statusCode: (int)HttpStatusCode.RequestTimeout
        ).ExecuteAsync(httpContext);

        // Return true to signal that this exception is handled
        return true;
    }
}