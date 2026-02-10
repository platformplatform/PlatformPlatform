using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.AccountManagement.Integrations.OAuth;

public sealed class OAuthProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
{
    public const string UseMockProviderCookieName = "__Test_Use_Mock_Provider";
    public const string MockEmailDomain = "@mock.localhost";

    private readonly bool _allowMockProvider = GetAllowMockProvider(configuration);

    public bool ShouldUseMockProvider(HttpContext httpContext)
    {
        if (!_allowMockProvider)
        {
            return false;
        }

        return httpContext.Request.Cookies.ContainsKey(UseMockProviderCookieName);
    }

    public IOAuthProvider? GetProvider(ExternalProviderType providerType, bool useMock)
    {
        if (useMock && !_allowMockProvider)
        {
            return null;
        }

        var serviceKey = useMock
            ? $"mock-{providerType.ToString().ToLowerInvariant()}"
            : providerType.ToString().ToLowerInvariant();

        return serviceProvider.GetKeyedService<IOAuthProvider>(serviceKey);
    }

    private static bool GetAllowMockProvider(IConfiguration configuration)
    {
        var allowMockProvider = configuration.GetValue<bool>("OAuth:AllowMockProvider");

        if (allowMockProvider && SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            throw new InvalidOperationException("Mock OAuth provider cannot be enabled in Azure environments.");
        }

        return allowMockProvider;
    }
}
