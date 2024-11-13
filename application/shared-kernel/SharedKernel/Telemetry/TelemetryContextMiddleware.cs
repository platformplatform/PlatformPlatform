using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public sealed class TelemetryContextMiddleware(IExecutionContext executionContext) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        // Set standard OpenTelemetry semantic convention for getting the geo data form the Client IP Address
        Activity.Current?.SetTag("client.address", executionContext.ClientIpAddress.ToString());

        await next(context);
    }
}
