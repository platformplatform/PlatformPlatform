using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Account.Integrations.Stripe;

/// <summary>
///     Resolves the platform currency once at application startup by reading the active Stripe price
///     catalog and validating that every active price uses the same currency. The resolved value is
///     cached on <see cref="PlatformCurrencyProvider" /> for the process lifetime — the platform
///     currency never changes at runtime. When Stripe is not configured (
///     <see cref="UnconfiguredStripeClient" /> is the active implementation) the provider stays
///     <c>null</c> and consumers handle the missing currency gracefully. When Stripe is configured but
///     resolution fails or returns no currency, startup aborts with a clear exception so the
///     application never serves a missing- or mixed-currency state.
/// </summary>
public sealed class PlatformCurrencyStartupResolver(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    PlatformCurrencyProvider platformCurrencyProvider,
    ILogger<PlatformCurrencyStartupResolver> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var stripeClient = ResolveActiveStripeClient(scope.ServiceProvider);

        if (stripeClient is UnconfiguredStripeClient)
        {
            logger.LogInformation("Stripe is not configured; platform currency will be null for the process lifetime");
            return;
        }

        var currency = await stripeClient.GetPlatformCurrencyAsync(cancellationToken);
        if (currency is null)
        {
            throw new InvalidOperationException("Stripe is configured but the platform currency could not be resolved from active prices.");
        }

        platformCurrencyProvider.SetCurrency(currency);
        logger.LogInformation("Resolved platform currency '{Currency}' from active Stripe prices", currency);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private IStripeClient ResolveActiveStripeClient(IServiceProvider scopedServiceProvider)
    {
        // The mock provider is gated per-request by an HTTP cookie at runtime; at startup there is no
        // request, so select directly based on configuration. The mock provider is preferred in test
        // and local-dev runs (Stripe:AllowMockProvider=true) so the resolver populates the provider
        // with the mock's configured currency. Otherwise pick the real Stripe client when configured,
        // or fall back to the unconfigured client.
        var allowMockProvider = configuration.GetValue<bool>("Stripe:AllowMockProvider");
        if (allowMockProvider)
        {
            return scopedServiceProvider.GetRequiredKeyedService<IStripeClient>("mock-stripe");
        }

        var isStripeSubscriptionEnabled = configuration["Stripe:SubscriptionEnabled"] == "true";
        if (isStripeSubscriptionEnabled)
        {
            return scopedServiceProvider.GetRequiredKeyedService<IStripeClient>("stripe");
        }

        return scopedServiceProvider.GetRequiredKeyedService<IStripeClient>("unconfigured-stripe");
    }
}
