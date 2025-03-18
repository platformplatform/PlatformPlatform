using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.Antiforgery;

public sealed class AntiforgeryMiddleware(IAntiforgery antiforgery, ILogger<AntiforgeryMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation == false)
        {
            // Skip validation for endpoints with disabled antiforgery
            await next(context);
            return;
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable("BypassAntiforgeryValidation"), out _))
        {
            logger.LogDebug("Bypassing antiforgery validation due to environment variable setting");
            await next(context);
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context))
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogWarning(
                "Antiforgery validation failed for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId
            );

            await Results.Problem(
                title: "Invalid Antiforgery Token",
                detail: "Antiforgery validation failed for request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { { "traceId", traceId } }
            ).ExecuteAsync(context);

            return;
        }

        await next(context);
    }
}
