using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class TimeoutExceptionHandler(
    ILogger<TimeoutExceptionHandler> logger,
    IOptions<JsonOptions> jsonOptions
) : IExceptionHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
    private readonly ILogger _logger = logger;

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

        _logger.LogError(exception, "An timeout exception occurred while processing the request.");

        httpContext.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)HttpStatusCode.RequestTimeout}",
            Title = "Request Timeout",
            Status = (int)HttpStatusCode.RequestTimeout,
            Detail = $"{httpContext.Request.Method} {httpContext.Request.Path} {httpContext.Request.QueryString}".Trim()
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, _jsonSerializerOptions);
        await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);

        // Return true to signal that this exception is handled
        return true;
    }
}