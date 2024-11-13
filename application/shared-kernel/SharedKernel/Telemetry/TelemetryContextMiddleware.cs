using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public sealed class TelemetryContextMiddleware(IExecutionContext executionContext, OpenTelemetryEnricher openTelemetryEnricher)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        openTelemetryEnricher.Apply();

        await next(context);
    }
}
