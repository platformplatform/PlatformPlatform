using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Account.Integrations.OAuth;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class StripeClientFactory(IServiceProvider serviceProvider, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
{
    private readonly bool _allowMockProvider = configuration.GetValue<bool>("Stripe:AllowMockProvider");
    private readonly string? _publishableKey = configuration["Stripe:PublishableKey"];

    public bool IsConfigured { get; } = configuration["Stripe:ApiKey"] is not null;

    public string? GetPublishableKey()
    {
        return _publishableKey;
    }

    public IStripeClient GetClient()
    {
        if (ShouldUseMockProvider())
        {
            return serviceProvider.GetRequiredKeyedService<IStripeClient>("mock-stripe");
        }

        if (IsConfigured)
        {
            return serviceProvider.GetRequiredKeyedService<IStripeClient>("stripe");
        }

        return serviceProvider.GetRequiredKeyedService<IStripeClient>("unconfigured-stripe");
    }

    private bool ShouldUseMockProvider()
    {
        if (!_allowMockProvider)
        {
            return false;
        }

        return httpContextAccessor.HttpContext?.Request.Cookies.ContainsKey(OAuthProviderFactory.UseMockProviderCookieName) == true;
    }
}
