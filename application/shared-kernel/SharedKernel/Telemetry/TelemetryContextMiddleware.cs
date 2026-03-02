using Microsoft.AspNetCore.Http;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Telemetry;

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
