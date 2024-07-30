using System.Text.Json;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NJsonSchema.Generation;
using PlatformPlatform.SharedKernel.ApiCore.Aspire;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApiCore.Filters;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;
using PlatformPlatform.SharedKernel.ApiCore.SchemaProcessor;
using PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;

namespace PlatformPlatform.SharedKernel.ApiCore;

public static class ApiCoreConfiguration
{
    private const string LocalhostCorsPolicyName = "LocalhostCorsPolicy";

    private static readonly string LocalhostUrl =
        Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)!;

    public static IServiceCollection AddApiCoreServices(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        Assembly apiAssembly,
        Assembly domainAssembly
    )
    {
        services.Scan(scan => scan
            .FromAssemblies(apiAssembly, Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes.AssignableTo<IEndpoints>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        services
            .AddExceptionHandler<TimeoutExceptionHandler>()
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddTransient<ModelBindingExceptionHandlerMiddleware>()
            .AddProblemDetails()
            .AddEndpointsApiExplorer();

        var applicationInsightsServiceOptions = new ApplicationInsightsServiceOptions
        {
            EnableRequestTrackingTelemetryModule = false,
            EnableDependencyTrackingTelemetryModule = false,
            RequestCollectionOptions = { TrackExceptions = false }
        };

        services.AddApplicationInsightsTelemetry(applicationInsightsServiceOptions);
        services.AddApplicationInsightsTelemetryProcessor<EndpointTelemetryFilter>();

        services.AddOpenApiDocument((settings, serviceProvider) =>
            {
                settings.DocumentName = "v1";
                settings.Title = "PlatformPlatform API";
                settings.Version = "v1";

                var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
                var serializerOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
                options.SerializerOptions = new JsonSerializerOptions(serializerOptions);

                // Ensure that enums are serialized as strings and use CamelCase
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                settings.DocumentProcessors.Add(new StronglyTypedDocumentProcessor(domainAssembly));
            }
        );

        // Add Authentication and Authorization services (This is new code, that has just been added)
        services.AddAuthentication().AddBearerToken();
        services.AddAuthorization();

        // Ensure that enums are serialized as strings
        services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }
        );

        // Ensure correct client IP addresses are set for requests
        // This is required when running behind a reverse proxy like YARP or Azure Container Apps
        services.Configure<ForwardedHeadersOptions>(options =>
            {
                // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
        );

        builder.AddServiceDefaults();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options => options.AddPolicy(
                    LocalhostCorsPolicyName,
                    policyBuilder => { policyBuilder.WithOrigins(LocalhostUrl).AllowAnyMethod().AllowAnyHeader(); }
                )
            );
        }
        else
        {
            builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; });
        }

        return services;
    }

    public static IServiceCollection ConfigureDevelopmentPort(this IServiceCollection services, WebApplicationBuilder builder, int port)
    {
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                if (!context.HostingEnvironment.IsDevelopment()) return;

                serverOptions.ConfigureEndpointDefaults(listenOptions => listenOptions.UseHttps());

                serverOptions.ListenLocalhost(port, listenOptions => listenOptions.UseHttps());
            }
        );

        return services;
    }

    public static WebApplication UseApiCoreConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Enable the developer exception page, which displays detailed information about exceptions that occur
            app.UseDeveloperExceptionPage();
            app.UseCors(LocalhostCorsPolicyName);
        }
        else
        {
            // Configure global exception handling for the production environment
            app.UseExceptionHandler(_ => { });
        }

        // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto. Should run before other middleware.
        app.UseForwardedHeaders();

        // Add Authentication and Authorization middleware (This is new code, that has just been added)
        app.UseAuthentication();
        app.UseAuthorization();

        // Enable Swagger UI
        app.UseOpenApi();
        app.UseSwaggerUi();

        app.UseMiddleware<ModelBindingExceptionHandlerMiddleware>();

        // Manually create all endpoints classes to call the MapEndpoints containing the mappings
        using var scope = app.Services.CreateScope();
        var endpointServices = scope.ServiceProvider.GetServices<IEndpoints>();
        foreach (var endpoint in endpointServices)
        {
            endpoint.MapEndpoints(app);
        }

        return app;
    }
}
