using System.Text;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Multi-source reconciliation behaviour: the live webhook flow runs
///     <c>ReconcileEventLogFromEventsListAsync</c> before the replayer so events that Stripe knows
///     about but we missed (dropped webhook delivery, our 5xx, network blip) get inserted into the
///     <c>stripe_events</c> archive as recovered rows. The replayer then reads from the local
///     archive — which is now complete — and emits the corresponding <c>billing_events</c>.
/// </summary>
public sealed class EventLogReconciliationTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    private void SetupSubscription(string? stripeSubscriptionId = MockStripeClient.MockSubscriptionId, string plan = nameof(SubscriptionPlan.Standard))
    {
        var hasStripeSubscription = stripeSubscriptionId is not null;
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", plan),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", stripeSubscriptionId),
                ("current_price_amount", hasStripeSubscription ? 29.99m : null),
                ("current_price_currency", hasStripeSubscription ? "DKK" : null),
                ("current_period_end", hasStripeSubscription ? TimeProvider.GetUtcNow().AddDays(30) : null)
            ]
        );
    }

    [Fact]
    public async Task Reconcile_WhenEventsListReturnsEventNotInLocalArchive_ShouldInsertAsRecovered()
    {
        // Arrange — fresh subscription with no recorded events. The MockStripeClient.GetEventsForCustomerAsync
        // returns a default customer.subscription.created event (id = MockStripeClient.MockSubscriptionCreatedEventId).
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));

        // Act — webhook arrives, triggers ProcessPendingStripeEvents which runs reconciliation
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert — the missing customer.subscription.created event should be inserted as recovered
        response.EnsureSuccessStatusCode();

        var recoveredCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM stripe_events WHERE id = @id AND recovery_source = @source",
            [new { id = MockStripeClient.MockSubscriptionCreatedEventId, source = "events_list" }]
        );
        recoveredCount.Should().Be(1, "the event Stripe knew about but we missed should be inserted with recovery_source = events_list");

        var status = Connection.ExecuteScalar<string>(
            "SELECT status FROM stripe_events WHERE id = @id",
            [new { id = MockStripeClient.MockSubscriptionCreatedEventId }]
        );
        status.Should().Be(nameof(StripeEventStatus.Processed), "recovered events land directly as Processed because the replayer picks them up immediately");

        var recoveredAt = Connection.ExecuteScalar<string>(
            "SELECT recovered_at FROM stripe_events WHERE id = @id",
            [new { id = MockStripeClient.MockSubscriptionCreatedEventId }]
        );
        recoveredAt.Should().NotBeNullOrEmpty("recovered_at marks this row as a webhook we didn't receive in real-time");
    }
}
