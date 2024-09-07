using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NJsonSchema.Generation;
using PlatformPlatform.SharedKernel.Aspire;
using PlatformPlatform.SharedKernel.Endpoints;
using PlatformPlatform.SharedKernel.Filters;
using PlatformPlatform.SharedKernel.Middleware;
using PlatformPlatform.SharedKernel.SchemaProcessor;
using PlatformPlatform.SharedKernel.SinglePageApp;

namespace PlatformPlatform.SharedKernel;

public static class ApiDependencyConfiguration
{
    private const string LocalhostCorsPolicyName = "LocalhostCorsPolicy";

    private static readonly string LocalhostUrl = Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)!;

    public static WebApplicationBuilder AddApiInfrastructure(
        this WebApplicationBuilder builder,
        Assembly apiAssembly,
        Assembly coreAssembly)
    {
        builder.Services.Scan(scan => scan
            .FromAssemblies(apiAssembly, Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes.AssignableTo<IEndpoints>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        builder.Services
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

        builder.Services.AddApplicationInsightsTelemetry(applicationInsightsServiceOptions);
        builder.Services.AddApplicationInsightsTelemetryProcessor<EndpointTelemetryFilter>();

        builder.Services.AddOpenApiDocument((settings, _) =>
            {
                settings.DocumentName = "v1";
                settings.Title = "PlatformPlatform API";
                settings.Version = "v1";

                var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
                options.SerializerOptions = SharedDependencyConfiguration.JsonSerializerOptions;
                settings.DocumentProcessors.Add(new StronglyTypedDocumentProcessor(coreAssembly));
            }
        );

        // Add Authentication and Authorization services
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }
        ).AddJwtBearer(o =>
            {
                var tokenSigningService = SharedDependencyConfiguration.GetTokenSigningService();
                o.TokenValidationParameters = tokenSigningService.GetTokenValidationParameters(
                    validateLifetime: true,
                    clockSkew: TimeSpan.FromSeconds(5) // In Azure, we don't need any clock skew, but this must be a higher value than the AppGateway
                );
            }
        );
        builder.Services.AddAuthorization();

        // Ensure that enums are serialized as strings
        builder.Services.Configure<JsonOptions>(options =>
            {
                foreach (var jsonConverter in SharedDependencyConfiguration.JsonSerializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(jsonConverter);
                }

                options.SerializerOptions.PropertyNamingPolicy = SharedDependencyConfiguration.JsonSerializerOptions.PropertyNamingPolicy;
            }
        );

        // Ensure correct client IP addresses are set for requests
        // This is required when running behind a reverse proxy like YARP or Azure Container Apps
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
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

        builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; });

        return builder;
    }

    public static WebApplicationBuilder AddDevelopmentPort(this WebApplicationBuilder builder, int port)
    {
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                if (!context.HostingEnvironment.IsDevelopment()) return;

                serverOptions.ConfigureEndpointDefaults(listenOptions => listenOptions.UseHttps());

                serverOptions.ListenLocalhost(port, listenOptions => listenOptions.UseHttps());
            }
        );

        return builder;
    }

    public static WebApplication UseApiServices(this WebApplication app)
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

        // Add Authentication and Authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Enable Swagger UI
        app.UseOpenApi();
        app.UseSwaggerUi();

        app.UseMiddleware<ModelBindingExceptionHandlerMiddleware>();

        // Manually create all endpoint classes to call the MapEndpoints containing the mappings
        using var scope = app.Services.CreateScope();
        var endpointServices = scope.ServiceProvider.GetServices<IEndpoints>();
        foreach (var endpoint in endpointServices)
        {
            endpoint.MapEndpoints(app);
        }

        return app;
    }
}
