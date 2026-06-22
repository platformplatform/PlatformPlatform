using Azure.Core;
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
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedKernel.Integrations.BlobStorage;
using SharedKernel.Telemetry;

namespace SharedKernel.Configuration;

public static class SharedInfrastructureConfiguration
{
    public static readonly bool IsRunningInAzure = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") is not null;

    public static readonly string? ServiceVersion = ResolveServiceVersion();

    // Baked into the assembly at build time via /p:DeploymentCommitHash=<sha>
    // /p:DeploymentGithubActionId=<run_id> in the deployment workflows. The Directory.Build.props at
    // /application emits AssemblyMetadata attributes from those properties so each artifact carries
    // its own provenance -- pulling and running the image anywhere produces the same telemetry stamp.
    public static readonly string? DeploymentCommitHash = GetAssemblyMetadata("DeploymentCommitHash");

    public static readonly string? DeploymentGithubActionId = GetAssemblyMetadata("DeploymentGithubActionId");

    public static DefaultAzureCredential DefaultAzureCredential => GetDefaultAzureCredential();

    private static string? ResolveServiceVersion()
    {
        // The .NET SDK auto-appends "+<source revision id>" to AssemblyInformationalVersion when
        // building inside a git checkout. Strip it so application_Version stays clean (the commit
        // SHA travels separately as the deployment.commit_hash custom dimension).
        var informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (informationalVersion is null) return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex < 0 ? informationalVersion : informationalVersion[..plusIndex];
    }

    private static string? GetAssemblyMetadata(string key)
    {
        return Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value;
    }

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
                .AddSharedTelemetry();

            builder.Services
                .ConfigureHttpClientDefaults(http =>
                    {
                        // 45s total request timeout for API-tier outbound calls (account-api, main-api).
                        // Sized to absorb a downstream container scale-from-zero: ~31s P99 container start
                        // plus ~12s P99 first-request slowness on account-api. Still tight enough to defend
                        // against slow-loris / DoS upstream. AppGateway sets a longer per-client timeout for
                        // its account-api refresh path because that path must deliver the rotated Set-Cookie
                        // to the browser.
                        // Retries are disabled: most 5xx errors during a bad release are persistent, so
                        // retrying triples the load during incidents and inflates the failure rate in
                        // Application Insights. Real transient errors are rare in same-region traffic.
                        http.AddStandardResilienceHandler(options =>
                            {
                                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
                                options.Retry.ShouldHandle = _ => ValueTask.FromResult(false);
                            }
                        );
                        http.AddServiceDiscovery(); // Turn on service discovery by default
                    }
                );

            return builder;
        }

        // Wires OpenTelemetry tracing/logging/metrics, the Azure Monitor exporter, and Application
        // Insights without requiring a DbContext. AppGateway and other database-less hosts should call
        // this directly; AddSharedInfrastructure<T> calls it as part of the full bundle.
        public IHostApplicationBuilder AddSharedTelemetry()
        {
            builder
                .AddConfigureOpenTelemetry()
                .AddOpenTelemetryExporters();

            // Register service.version AFTER UseAzureMonitor so it wins the resource merge against
            // the Azure Container Apps detector, which otherwise sets service.version to the revision
            // name. The deployment.* tags travel as activity tags via DeploymentTagsProcessor since
            // Azure Monitor only surfaces a fixed set of resource attributes on AppRequests.
            if (ServiceVersion is not null)
            {
                builder.Services.AddOpenTelemetry().ConfigureResource(resource =>
                    resource.AddAttributes([new KeyValuePair<string, object>("service.version", ServiceVersion)])
                );
            }

            builder.Services.AddApplicationInsightsTelemetry();

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

            if (IsRunningInAzure)
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                dataSourceBuilder.UsePeriodicPasswordProvider(async (_, cancellationToken) =>
                    {
                        var token = await DefaultAzureCredential.GetTokenAsync(new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), cancellationToken);
                        return token.Token;
                    }, TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(5)
                );
                var dataSource = dataSourceBuilder.Build();
                builder.Services.AddSingleton(dataSource);
                builder.Services.AddDbContext<T>(options =>
                    options.UseNpgsql(dataSource, o => o.MigrationsHistoryTable("__ef_migrations_history")).UseSnakeCaseNamingConvention()
                );
            }
            else
            {
                builder.Services.AddDbContext<T>(options =>
                    options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__ef_migrations_history")).UseSnakeCaseNamingConvention()
                );
            }

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

                        if (EndpointTelemetryFilter.ExcludedPaths.Any(requestPath.StartsWith))
                        {
                            return false;
                        }

                        if (EndpointTelemetryFilter.ExcludedFileExtensions.Any(requestPath.EndsWith))
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

                        tracing
                            .AddProcessor(new DeploymentTagsProcessor())
                            .AddAspNetCoreInstrumentation(options =>
                                {
                                    options.EnrichWithHttpRequest = PublicHostTelemetryEnricher.Enrich;
                                    // 4xx is a client problem (validation, auth, missing route); the server handled it.
                                    // Mark OK so Application Insights doesn't flag it; only 5xx is a real server error.
                                    options.EnrichWithHttpResponse = (activity, response) =>
                                    {
                                        if (response.StatusCode is >= 400 and < 500) activity.SetStatus(ActivityStatusCode.Ok);
                                    };
                                }
                            )
                            .AddGrpcClientInstrumentation()
                            .AddHttpClientInstrumentation(options =>
                                options.EnrichWithHttpRequestMessage = PublicHostTelemetryEnricher.EnrichOutbound
                            );
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

            // Azure Monitor exports to Application Insights only from Azure-hosted instances. Outside Azure the
            // connection string is a localhost placeholder, so the exporter and Live Metrics have nothing real to
            // reach; their background flush then blocks host shutdown (e.g. WebApplicationFactory teardown in
            // integration tests). Only wire it when running in Azure.
            if (IsRunningInAzure)
            {
                builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
                    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                );
            }

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

            services
                .AddApplicationInsightsTelemetry(applicationInsightsServiceOptions)
                .AddApplicationInsightsTelemetryProcessor<EndpointTelemetryFilter>()
                .AddSingleton<ITelemetryInitializer, ApplicationInsightsTelemetryInitializer>();

            if (!IsRunningInAzure)
            {
                services.AddApplicationInsightsTelemetryProcessor<DevelopmentApplicationInsightsLogger>();
            }

            return services;
        }
    }
}
