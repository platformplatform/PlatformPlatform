using System.Text.Json;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Behaviors;
using PlatformPlatform.SharedKernel.DomainEvents;
using PlatformPlatform.SharedKernel.Persistence;
using PlatformPlatform.SharedKernel.Services;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.SharedKernel;

public static class SharedDependencyConfiguration
{
    // Ensure that enums are serialized as strings and use CamelCase
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddSharedServices<T>(this IServiceCollection services)
        where T : DbContext
    {
        // Even though the HttpContextAccessor is not available in Worker Services, it is still registered here because
        // workers register the same CommandHandlers as the API, which may require the HttpContext.
        // Consider making a generic IRequestContextProvider that can return the HttpContext only if it is available.
        services.AddHttpContextAccessor();

        services
            .AddSingleton(GetTokenSigningService())
            .AddServiceDiscovery()
            .AddDefaultJsonSerializerOptions()
            .AddPersistenceHelpers<T>()
            .AddMediatRPipelineBehaviours()
            .AddDefaultHealthChecks()
            .AddEmailSignatureService();

        return services;
    }

    public static IServiceCollection AddProjectServices(this IServiceCollection services, Assembly assembly)
    {
        services
            .RegisterMediatRRequest(assembly)
            .RegisterRepositories(assembly);

        return services;
    }

    public static ITokenSigningService GetTokenSigningService()
    {
        if (SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
            var keyClient = new KeyClient(keyVaultUri, SharedInfrastructureConfiguration.DefaultAzureCredential);
            var cryptographyClient = new CryptographyClient(
                keyClient.GetKey("authentication-token-signing-key").Value.Id,
                SharedInfrastructureConfiguration.DefaultAzureCredential
            );

            var secretClient = new SecretClient(keyVaultUri, SharedInfrastructureConfiguration.DefaultAzureCredential);
            var issuer = secretClient.GetSecret("authentication-token-issuer").Value.Value;
            var audience = secretClient.GetSecret("authentication-token-audience").Value.Value;

            return new AzureTokenSigningService(cryptographyClient, issuer, audience);
        }

        return new DevelopmentTokenSigningService();
    }

    private static IServiceCollection AddDefaultJsonSerializerOptions(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
            {
                // Copy the default options from the DefaultJsonSerializerOptions to enforce consistency in serialization.
                foreach (var jsonConverter in DefaultJsonSerializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(jsonConverter);
                }

                options.SerializerOptions.PropertyNamingPolicy = DefaultJsonSerializerOptions.PropertyNamingPolicy;
            }
        );
        return services;
    }

    private static IServiceCollection AddPersistenceHelpers<T>(this IServiceCollection services) where T : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));
        services.AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
            new DomainEventCollector(provider.GetRequiredService<T>())
        );
        return services;
    }

    private static IServiceCollection AddMediatRPipelineBehaviours(this IServiceCollection services)
    {
        // Order is important! First all Pre-behaviors run, then the command is handled, and finally all Post behaviors run.
        // So Validation → Command → PublishDomainEvents → UnitOfWork → PublishTelemetryEvents.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>();
        services.AddScoped<ConcurrentCommandCounter>();

        return services;
    }

    private static IServiceCollection RegisterMediatRRequest(this IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type =>
                    type.IsClass && (type.IsNotPublic || type.IsPublic)
                                 && type.BaseType is { IsGenericType: true } &&
                                 type.BaseType.GetGenericTypeDefinition() == typeof(RepositoryBase<,>)
                )
            )
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }

    private static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        // Add a default liveness check to ensure the app is responsive
        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return services;
    }

    private static IServiceCollection AddEmailSignatureService(this IServiceCollection services)
    {
        if (SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
            services.AddSingleton(_ => new SecretClient(keyVaultUri, SharedInfrastructureConfiguration.DefaultAzureCredential));
            services.AddTransient<IEmailService, AzureEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, DevelopmentEmailService>();
        }

        return services;
    }
}
