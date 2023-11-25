using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
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
        _logger.LogError(exception, "An error occurred while processing the request.");

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)HttpStatusCode.InternalServerError}",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "An error occurred while processing the request."
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, _jsonSerializerOptions);
        await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);

        // Return true to signal that this exception is handled
        return true;
    }
}