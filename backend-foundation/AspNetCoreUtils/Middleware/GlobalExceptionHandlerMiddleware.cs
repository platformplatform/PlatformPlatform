using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PlatformPlatform.Foundation.AspNetCoreUtils.Middleware;

public sealed class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger _logger;

    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request");

            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{(int)HttpStatusCode.InternalServerError}",
                Title = "Server Error",
                Status = (int) HttpStatusCode.InternalServerError,
                Detail = "An error occurred while processing the request."
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}