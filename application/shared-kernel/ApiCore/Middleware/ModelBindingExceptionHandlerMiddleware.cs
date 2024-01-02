using System.Net;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class ModelBindingExceptionHandlerMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException exception)
        {
            await Results.Problem(
                title: "Bad Request",
                detail: exception.Message,
                statusCode: (int)HttpStatusCode.BadRequest
            ).ExecuteAsync(context);
        }
    }
}