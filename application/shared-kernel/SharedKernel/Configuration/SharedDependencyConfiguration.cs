using System.Text.Json;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Authentication.TokenSigning;
using PlatformPlatform.SharedKernel.DomainEvents;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Persistence;
using PlatformPlatform.SharedKernel.PipelineBehaviors;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.SharedKernel.Configuration;

public static class SharedDependencyConfiguration
{
    // Ensure that enums are serialized as strings and use CamelCase
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddSharedServices<T>(this IServiceCollection services, params Assembly[] assemblies)
        where T : DbContext
    {
        // Even though the HttpContextAccessor is not available in Worker Services, it is still registered here because
        // workers register the same CommandHandlers as the API, which may require the HttpContext.
        // Consider making a generic IRequestContextProvider that can return the HttpContext only if it is available.
        services.AddHttpContextAccessor();

        return services
            .AddServiceDiscovery()
            .AddSingleton(GetTokenSigningService())
            .AddAuthentication()
            .AddDefaultJsonSerializerOptions()
            .AddPersistenceHelpers<T>()
            .AddDefaultHealthChecks()
            .AddEmailClient()
            .AddMediatRPipelineBehaviors()
            .RegisterMediatRRequest(assemblies)
            .RegisterRepositories(assemblies);
    }

    public static ITokenSigningClient GetTokenSigningService()
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

            return new AzureTokenSigningClient(cryptographyClient, issuer, audience);
        }

        return new DevelopmentTokenSigningClient();
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services)
    {
        return services
            .AddScoped<IPasswordHasher<object>, PasswordHasher<object>>()
            .AddScoped<OneTimePasswordHelper>()
            .AddScoped<RefreshTokenGenerator>()
            .AddScoped<AccessTokenGenerator>()
            .AddScoped<AuthenticationTokenService>();
    }

    private static IServiceCollection AddDefaultJsonSerializerOptions(this IServiceCollection services)
    {
        return services.Configure<JsonOptions>(options =>
            {
                // Copy the default options from the DefaultJsonSerializerOptions to enforce consistency in serialization.
                foreach (var jsonConverter in DefaultJsonSerializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(jsonConverter);
                }

                options.SerializerOptions.PropertyNamingPolicy = DefaultJsonSerializerOptions.PropertyNamingPolicy;
            }
        );
    }

    private static IServiceCollection AddPersistenceHelpers<T>(this IServiceCollection services) where T : DbContext
    {
        return services
            .AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()))
            .AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
                new DomainEventCollector(provider.GetRequiredService<T>())
            );
    }

    private static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        // Add a default liveness check to ensure the app is responsive
        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return services;
    }

    private static IServiceCollection AddEmailClient(this IServiceCollection services)
    {
        if (SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
            services
                .AddSingleton(_ => new SecretClient(keyVaultUri, SharedInfrastructureConfiguration.DefaultAzureCredential))
                .AddTransient<IEmailClient, AzureEmailClient>();
        }
        else
        {
            services.AddTransient<IEmailClient, DevelopmentEmailClient>();
        }

        return services;
    }

    private static IServiceCollection AddMediatRPipelineBehaviors(this IServiceCollection services)
    {
        // Order is important! First all Pre behaviors run, then the command is handled, and finally all Post behaviors run.
        // So Validation → Command → PublishDomainEvents → UnitOfWork → PublishTelemetryEvents.
        services
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)) // Pre
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)) // Post
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)) // Post
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post

        return services
            .AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>()
            .AddScoped<ConcurrentCommandCounter>();
    }

    private static IServiceCollection RegisterMediatRRequest(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services
            .AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assemblies))
            .AddValidatorsFromAssemblies(assemblies);
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        return services
            .Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.Where(type =>
                        type.BaseType is { IsGenericType: true } &&
                        type.BaseType.GetGenericTypeDefinition() == typeof(RepositoryBase<,>)
                    ), false
                )
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            );
    }
}
