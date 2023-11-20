using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class ModelBindingExceptionHandlerMiddleware : IMiddleware
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ModelBindingExceptionHandlerMiddleware(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{(int)HttpStatusCode.BadRequest}",
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = ex.Message
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails, _jsonSerializerOptions);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}