using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PlatformPlatform.SharedKernel.ApiCore.Filters;

namespace PlatformPlatform.SharedKernel.ApiCore.Aspire;

public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            }
        );

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            {
                options.Filter = httpContext =>
                {
                    // Add filtering to exclude health check endpoints
                    var requestPath = httpContext.Request.Path.ToString();
                    return !Array.Exists(EndpointTelemetryFilter.ExcludedPaths, requestPath.StartsWith);
                };
            }
        );

        builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            }
        );

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                }
            )
            .WithTracing(tracing =>
                {
                    // We want to view all traces in development
                    if (builder.Environment.IsDevelopment()) tracing.SetSampler(new AlwaysOnSampler());

                    tracing.AddAspNetCoreInstrumentation().AddGrpcClientInstrumentation().AddHttpClientInstrumentation();
                }
            );

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ??
                                           "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost";
            }
        );

        return builder;
    }

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        // Add a default liveness check to ensure app is responsive
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
