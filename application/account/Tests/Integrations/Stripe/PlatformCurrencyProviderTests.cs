using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SharedKernel.Telemetry;
using Xunit;
using AccountStripeClient = Account.Integrations.Stripe.StripeClient;
using IStripeClient = Account.Integrations.Stripe.IStripeClient;
using StripePrice = Stripe.Price;

namespace Account.Tests.Integrations.Stripe;

// Pins the platform-currency resolution contract: the startup resolver populates the singleton
// provider exactly once from the active Stripe client, validates the single-currency invariant
// (every active price must use the same currency), and remains null when Stripe is not configured.
// These tests are the structural backstop for the dashboard MRR handlers, which sum decimal amounts
// without grouping by currency and therefore depend on this invariant holding.
public sealed class PlatformCurrencyProviderTests
{
    [Fact]
    public async Task StartAsync_WithMockStripeClient_ShouldResolveSingleCurrencyOntoProvider()
    {
        // Happy path: the mock client returns its configured currency from GetPlatformCurrencyAsync,
        // the resolver caches it on the singleton provider for the process lifetime.
        // Arrange
        const string expectedCurrency = MockStripeClient.MockStandardCurrency;
        var provider = new PlatformCurrencyProvider();
        var configuration = BuildConfiguration(("Stripe:AllowMockProvider", "true"));
        var services = BuildServiceProviderWithMockStripeClient(configuration, expectedCurrency);
        var resolver = new PlatformCurrencyStartupResolver(services, configuration, provider, NullLogger<PlatformCurrencyStartupResolver>.Instance);

        // Act
        await resolver.StartAsync(CancellationToken.None);

        // Assert
        provider.Currency.Should().Be(expectedCurrency);
    }

    [Fact]
    public async Task StartAsync_WithStripeClientObservingMultipleCurrencies_ShouldThrow()
    {
        // The real StripeClient validates uniform currency across the active price catalog. When two
        // active prices disagree, the resolver must fail fast at startup so the application never
        // serves a mixed-currency state.
        // Arrange
        var provider = new PlatformCurrencyProvider();
        var configuration = BuildConfiguration(("Stripe:SubscriptionEnabled", "true"));
        var services = BuildServiceProviderWithStripeClientCachingMixedCurrencyPrices(configuration);
        var resolver = new PlatformCurrencyStartupResolver(services, configuration, provider, NullLogger<PlatformCurrencyStartupResolver>.Instance);

        // Act
        var act = () => resolver.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*multiple currencies*");
        provider.Currency.Should().BeNull("the provider must not be populated when the invariant is violated");
    }

    [Fact]
    public async Task MockStripeClient_WhenResolverHasPopulatedCurrency_ShouldEmitProviderCurrencyFromSyncSubscriptionState()
    {
        // Prove the mock tracks the resolver-populated platform currency at request time rather than
        // emitting its hardcoded default. Without this, a developer's Stripe sandbox configured with
        // USD (or any non-default currency) would cache USD on the provider but the mock would still
        // emit DKK from SyncSubscriptionStateAsync, tripping StripeSubscriptionCurrencyMismatchRejected
        // on every mock-cookie subscribe attempt. Routes the resolver-driven seed through the public
        // PlatformCurrencyStartupResolver path so the test does not poke at the provider's internal
        // setter — the mock's GetPlatformCurrencyAsync returns "USD" because SubscriptionCurrency wins
        // over the (still-null) PlatformCurrencyProvider.Currency.
        // Arrange
        var configuration = BuildConfiguration(("Stripe:AllowMockProvider", "true"));
        var provider = new PlatformCurrencyProvider();
        var mockState = new MockStripeState { SubscriptionCurrency = "USD", PlatformCurrencyProvider = provider };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(mockState);
        services.AddKeyedScoped<IStripeClient, MockStripeClient>("mock-stripe");
        var serviceProvider = services.BuildServiceProvider();
        var resolver = new PlatformCurrencyStartupResolver(serviceProvider, configuration, provider, NullLogger<PlatformCurrencyStartupResolver>.Instance);
        await resolver.StartAsync(CancellationToken.None);
        mockState.SubscriptionCurrency = null;
        var mockStripeClient = serviceProvider.GetRequiredKeyedService<IStripeClient>("mock-stripe");

        // Act
        var syncResult = await mockStripeClient.SyncSubscriptionStateAsync(StripeCustomerId.NewId(MockStripeClient.MockCustomerId), CancellationToken.None);

        // Assert
        provider.Currency.Should().Be("USD", "the resolver must have cached the mock's seed");
        syncResult.Should().NotBeNull();
        syncResult.CurrentPriceCurrency.Should().Be("USD", "the mock must surface the resolver-populated currency from SyncSubscriptionStateAsync so the boundary guard does not fire on every mock-cookie subscribe in a non-DKK Stripe sandbox");
    }

    [Fact]
    public async Task StartAsync_WithUnconfiguredStripeClient_ShouldLeaveProviderCurrencyNull()
    {
        // When Stripe is not configured the resolver returns early without touching the provider;
        // dashboard handlers render gracefully when currency is missing.
        // Arrange
        var provider = new PlatformCurrencyProvider();
        var configuration = BuildConfiguration();
        var services = BuildServiceProviderWithUnconfiguredStripeClient();
        var resolver = new PlatformCurrencyStartupResolver(services, configuration, provider, NullLogger<PlatformCurrencyStartupResolver>.Instance);

        // Act
        await resolver.StartAsync(CancellationToken.None);

        // Assert
        provider.Currency.Should().BeNull();
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] entries)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();
    }

    private static IServiceProvider BuildServiceProviderWithMockStripeClient(IConfiguration configuration, string currency)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new MockStripeState { SubscriptionCurrency = currency });
        services.AddKeyedScoped<IStripeClient, MockStripeClient>("mock-stripe");
        return services.BuildServiceProvider();
    }

    private static IServiceProvider BuildServiceProviderWithStripeClientCachingMixedCurrencyPrices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton<IMemoryCache>(_ =>
            {
                var cache = new MemoryCache(new MemoryCacheOptions());
                // Pre-populate the catalog cache with two active prices disagreeing on currency. The
                // resolver's GetPlatformCurrencyAsync reads this cache (under the same private key) and
                // throws on observing more than one distinct currency. Keys match the StripeClient
                // lookup-key contract (standard_monthly, premium_monthly).
                cache.Set(
                    "stripe_resolved_prices",
                    new Dictionary<string, StripePrice>
                    {
                        ["standard_monthly"] = new() { Id = "price_standard", Currency = "dkk", ProductId = "prod_standard" },
                        ["premium_monthly"] = new() { Id = "price_premium", Currency = "usd", ProductId = "prod_premium" }
                    },
                    TimeSpan.FromMinutes(1)
                );
                return cache;
            }
        );
        services.AddSingleton<IPlatformCurrencyProvider>(new PlatformCurrencyProvider());
        services.AddSingleton(Substitute.For<ITelemetryEventsCollector>());
        services.AddKeyedScoped<IStripeClient, AccountStripeClient>("stripe");
        return services.BuildServiceProvider();
    }

    private static IServiceProvider BuildServiceProviderWithUnconfiguredStripeClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeyedScoped<IStripeClient, UnconfiguredStripeClient>("unconfigured-stripe");
        return services.BuildServiceProvider();
    }
}
