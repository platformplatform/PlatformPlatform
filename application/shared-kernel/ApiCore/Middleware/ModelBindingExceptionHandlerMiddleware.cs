using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class ModelBindingExceptionHandlerMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException ex)
        {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{(int) HttpStatusCode.BadRequest}",
                Title = "Bad Request",
                Status = (int) HttpStatusCode.BadRequest,
                Detail = ex.Message
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}