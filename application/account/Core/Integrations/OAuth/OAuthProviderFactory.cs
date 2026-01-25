using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.Account.Integrations.OAuth;

public sealed class OAuthProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
{
    public const string UseMockProviderCookieName = "__Test_Use_Mock_Provider";

    private readonly bool _allowMockProvider = configuration.GetValue<bool>("OAuth:AllowMockProvider");

    public bool ShouldUseMockProvider(HttpContext httpContext)
    {
        if (!_allowMockProvider)
        {
            return false;
        }

        return httpContext.Request.Cookies.ContainsKey(UseMockProviderCookieName);
    }

    public string? GetMockEmailPrefix(HttpContext httpContext)
    {
        if (!_allowMockProvider)
        {
            return null;
        }

        var cookieValue = httpContext.Request.Cookies[UseMockProviderCookieName];
        return cookieValue is not null && cookieValue != "true" ? cookieValue : null;
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
}
