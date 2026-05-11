using System.Text;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Regression suite for the .Single → .SingleOrDefault catalog-gap fix. When Stripe's upstream
///     price-list call returns nothing (empty cache after a transient failure) the scheduled-price
///     lookup in ProcessPendingStripeEvents.cs used to throw InvalidOperationException, rolling back
///     the entire webhook transaction and poisoning the hot path (Stripe retries indefinitely) as
///     well as the admin reconcile (opaque 500 at exactly the worst time). The fix skips the
///     ScheduledPriceAmount write when the result is null, emits a structured warning + the
///     StripePriceCatalogLookupMissed telemetry event, and lets the transaction commit normally.
/// </summary>
public sealed class ProcessPendingStripeEventsCatalogGapTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    private void SetupSubscription(string plan = nameof(SubscriptionPlan.Standard))
    {
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", plan),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEmpty_DowngradeScheduledPathSkipsScheduledPriceWrite()
    {
        // Webhook hot path: local has no scheduled plan, Stripe reports a newly scheduled downgrade to
        // Premium, and the price catalog is empty (simulating an upstream Stripe price-list failure
        // that left the in-memory cache unpopulated). The old .Single() threw InvalidOperationException
        // and rolled the transaction back, causing Stripe to retry the webhook indefinitely.
        // Arrange
        SetupSubscription();
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("scheduled_plan", null),
                ("scheduled_price_amount", null)
            ]
        );
        StripeState.ScheduledPlan = SubscriptionPlan.Premium;
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Standard);
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Premium);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var scheduledPriceAmount = Connection.ExecuteScalar<string>(
            $"SELECT scheduled_price_amount FROM subscriptions WHERE tenant_id = {DatabaseSeeder.Tenant1.Id.Value}", []
        );
        scheduledPriceAmount.Should().BeNullOrEmpty("the catalog-gap path must skip the ScheduledPriceAmount write so the empty cache scenario does not roll the transaction back");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "StripePriceCatalogLookupMissed", "a structured catalog-gap telemetry event must be emitted so operators see the failure");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEmpty_DowngradeCancelledPathSkipsScheduledPriceWrite()
    {
        // Webhook hot path: local has a scheduled downgrade to Premium, Stripe reports that the
        // scheduled downgrade has been cancelled (Stripe ScheduledPlan is null), and the price
        // catalog is empty. The catalog lookup in this branch only drives the
        // SubscriptionDowngradeCancelled telemetry MRR impact; the SetScheduledPlan(..., null) call
        // above the lookup clears ScheduledPriceAmount as part of cancelling the downgrade, which
        // is the correct semantic. The catalog gap must not throw and must not roll back.
        // Arrange
        SetupSubscription();
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("scheduled_plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_price_amount", MockStripeClient.PremiumAmountExcludingTax)
            ]
        );
        StripeState.ScheduledPlan = null;
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Standard);
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Premium);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "StripePriceCatalogLookupMissed", "a structured catalog-gap telemetry event must be emitted so operators see the failure");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().NotContain(e => e.GetType().Name == "SubscriptionDowngradeCancelled", "the downgrade-cancelled telemetry event has no meaningful MRR impact without the catalog price, so it is skipped in the catalog-gap path");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEmpty_UnconditionalReconciliationSkipsScheduledPriceWrite()
    {
        // Webhook hot path: local already has ScheduledPlan=Premium with a known good price, Stripe
        // reports the same ScheduledPlan, and the price catalog is empty. The diff-based transition
        // detector does not fire (neither downgradeScheduled nor downgradeCancelled), so the
        // unconditional reconciliation block is the only catalog lookup in play. Skipping the write
        // in this branch preserves the previously-good ScheduledPriceAmount instead of overwriting
        // it with NULL on a transient catalog miss.
        // Arrange
        SetupSubscription();
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("scheduled_plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_price_amount", MockStripeClient.PremiumAmountExcludingTax)
            ]
        );
        StripeState.ScheduledPlan = SubscriptionPlan.Premium;
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Standard);
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Premium);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var scheduledPriceAmount = Connection.ExecuteScalar<string>(
            $"SELECT scheduled_price_amount FROM subscriptions WHERE tenant_id = {DatabaseSeeder.Tenant1.Id.Value}", []
        );
        scheduledPriceAmount.Should().NotBeNullOrEmpty("the previously-good ScheduledPriceAmount must remain untouched when the catalog lookup misses, not be overwritten with NULL");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "StripePriceCatalogLookupMissed", "a structured catalog-gap telemetry event must be emitted so operators see the failure");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogPartial_ResolvesAvailableButSkipsMissing()
    {
        // Partial catalog: Premium price is missing but Standard is present. The downgradeScheduled
        // branch looks up Premium (the scheduled plan) so it must take the catalog-gap path and
        // skip the write. A separate scenario where the lookup target IS present would resolve
        // normally — covered implicitly by the existing happy-path tests.
        // Arrange
        SetupSubscription();
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("scheduled_plan", null),
                ("scheduled_price_amount", null)
            ]
        );
        StripeState.ScheduledPlan = SubscriptionPlan.Premium;
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Premium);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var scheduledPriceAmount = Connection.ExecuteScalar<string>(
            $"SELECT scheduled_price_amount FROM subscriptions WHERE tenant_id = {DatabaseSeeder.Tenant1.Id.Value}", []
        );
        scheduledPriceAmount.Should().BeNullOrEmpty("with Premium missing from the partial catalog, the scheduled-plan price write must be skipped");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "StripePriceCatalogLookupMissed", "the catalog-gap telemetry event must fire when the targeted plan is missing even though other plans are present");
    }
}
