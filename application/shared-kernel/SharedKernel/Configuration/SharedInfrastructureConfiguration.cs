using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PlatformPlatform.SharedKernel.Integrations.BlobStorage;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.SharedKernel.Configuration;

public static class SharedInfrastructureConfiguration
{
    public static readonly bool IsRunningInAzure = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") is not null;

    public static DefaultAzureCredential DefaultAzureCredential => GetDefaultAzureCredential();

    private static DefaultAzureCredential GetDefaultAzureCredential()
    {
        // Hack: Remove trailing whitespace from the environment variable, added in Bicep to workaround issue #157.
        var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")!.Trim();
        var credentialOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId };
        return new DefaultAzureCredential(credentialOptions);
    }

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddSharedInfrastructure<T>(string connectionName)
            where T : DbContext
        {
            builder
                .AddAzureKeyVaultConfiguration()
                .ConfigureDatabaseContext<T>(connectionName)
                .AddDefaultBlobStorage()
                .AddConfigureOpenTelemetry()
                .AddOpenTelemetryExporters();

            builder.Services
                .AddApplicationInsightsTelemetry()
                .ConfigureHttpClientDefaults(http =>
                    {
                        http.AddStandardResilienceHandler(); // Turn on resilience by default
                        http.AddServiceDiscovery(); // Turn on service discovery by default
                    }
                );

            return builder;
        }
    }

    extension(IHostApplicationBuilder builder)
    {
        private IHostApplicationBuilder AddAzureKeyVaultConfiguration()
        {
            if (IsRunningInAzure)
            {
                var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
                var secretClient = new SecretClient(keyVaultUri, DefaultAzureCredential);

                builder.Configuration.AddAzureKeyVault(secretClient, new AzureKeyVaultConfigurationOptions
                    {
                        Manager = new KeyVaultSecretManager(),
                        ReloadInterval = TimeSpan.FromMinutes(1)
                    }
                );
            }

            return builder;
        }

        private IHostApplicationBuilder ConfigureDatabaseContext<T>(string connectionName)
            where T : DbContext
        {
            var connectionString = IsRunningInAzure
                ? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
                : builder.Configuration.GetConnectionString(connectionName);

            builder.Services.AddDbContext<T>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.UseCompatibilityLevel(150) // SQL Server 2019 compatibility to avoid native JSON type
                )
            );

            return builder;
        }

        private IHostApplicationBuilder AddDefaultBlobStorage()
        {
            // Register the default storage account for BlobStorage
            if (IsRunningInAzure)
            {
                var defaultBlobStorageUri = new Uri(Environment.GetEnvironmentVariable("BLOB_STORAGE_URL")!);
                builder.Services.AddSingleton<IBlobStorageClient>(sp =>
                    new BlobStorageClient(new BlobServiceClient(defaultBlobStorageUri, DefaultAzureCredential), sp.GetRequiredService<TimeProvider>())
                );
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("blob-storage");
                builder.Services.AddSingleton<IBlobStorageClient>(sp =>
                    new BlobStorageClient(new BlobServiceClient(connectionString), sp.GetRequiredService<TimeProvider>())
                );
            }

            return builder;
        }

        /// <summary>
        ///     Register different storage accounts for BlobStorage using .NET Keyed services, when a service needs to access
        ///     multiple storage accounts.
        /// </summary>
        public IHostApplicationBuilder AddNamedBlobStorages((string ConnectionName, string EnvironmentVariable)?[] connections)
        {
            if (IsRunningInAzure)
            {
                foreach (var connection in connections)
                {
                    var storageEndpointUri = new Uri(Environment.GetEnvironmentVariable(connection!.Value.EnvironmentVariable)!);
                    builder.Services.AddKeyedSingleton<IBlobStorageClient>(connection.Value.ConnectionName,
                        (sp, _) => new BlobStorageClient(new BlobServiceClient(storageEndpointUri, DefaultAzureCredential), sp.GetRequiredService<TimeProvider>())
                    );
                }
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("blob-storage");
                foreach (var connection in connections)
                {
                    builder.Services.AddKeyedSingleton<IBlobStorageClient>(connection!.Value.ConnectionName,
                        (sp, _) => new BlobStorageClient(new BlobServiceClient(connectionString), sp.GetRequiredService<TimeProvider>())
                    );
                }
            }

            return builder;
        }

        private IHostApplicationBuilder AddConfigureOpenTelemetry()
        {
            builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
                {
                    // ReSharper disable once RedundantLambdaParameterType
                    options.Filter = (HttpContext httpContext) =>
                    {
                        var requestPath = httpContext.Request.Path.ToString();

                        if (EndpointTelemetryFilter.ExcludedPaths.Any(excludePath => requestPath.StartsWith(excludePath)))
                        {
                            return false;
                        }

                        if (EndpointTelemetryFilter.ExcludedFileExtensions.Any(excludeExtension => requestPath.EndsWith(excludeExtension)))
                        {
                            return false;
                        }

                        return true;
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

            return builder;
        }

        private IHostApplicationBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services
                    .Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter())
                    .ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter())
                    .ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
            }

            builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
                {
                    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ??
                                               "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost";
                }
            );

            return builder;
        }
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddApplicationInsightsTelemetry()
        {
            var applicationInsightsServiceOptions = new ApplicationInsightsServiceOptions
            {
                EnableQuickPulseMetricStream = false,
                EnableRequestTrackingTelemetryModule = false,
                EnableDependencyTrackingTelemetryModule = false,
                RequestCollectionOptions = { TrackExceptions = false }
            };

            return services
                .AddApplicationInsightsTelemetry(applicationInsightsServiceOptions)
                .AddApplicationInsightsTelemetryProcessor<EndpointTelemetryFilter>()
                .AddScoped<OpenTelemetryEnricher>()
                .AddSingleton<ITelemetryInitializer, ApplicationInsightsTelemetryInitializer>();
        }
    }
}
