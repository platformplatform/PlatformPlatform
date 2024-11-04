using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.Middleware;

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
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            await Results.Problem(
                title: "Bad Request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { { "traceId", traceId } }
            ).ExecuteAsync(context);
        }
    }
}
