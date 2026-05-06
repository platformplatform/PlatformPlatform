using System.Text;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

public sealed class BillingEventAppendTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    private void SetupSubscription(string? stripeSubscriptionId = MockStripeClient.MockSubscriptionId, string plan = nameof(SubscriptionPlan.Standard), DateTimeOffset? firstPaymentFailedAt = null)
    {
        var hasStripeSubscription = stripeSubscriptionId is not null;
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", plan),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", stripeSubscriptionId),
                ("current_price_amount", hasStripeSubscription ? 29.99m : null),
                ("current_price_currency", hasStripeSubscription ? "USD" : null),
                ("current_period_end", hasStripeSubscription ? TimeProvider.GetUtcNow().AddDays(30) : null),
                ("first_payment_failed_at", firstPaymentFailedAt)
            ]
        );
    }

    [Fact]
    public async Task AppendBillingEvent_WhenSubscriptionCreated_ShouldAppendSubscriptionCreatedRow()
    {
        // Arrange — tenant on free plan, no Stripe subscription yet
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var billingEventCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionCreated) }]
        );
        billingEventCount.Should().Be(1, "the sync should have appended exactly one SubscriptionCreated row to the billing_events log");

        var toPlan = Connection.ExecuteScalar<string>(
            "SELECT to_plan FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionCreated) }]
        );
        toPlan.Should().NotBeNullOrEmpty("SubscriptionCreated should record the plan that was activated");
    }

    [Fact]
    public async Task AppendBillingEvent_WhenFirstPaymentFails_ShouldAppendPaymentFailedRow()
    {
        // Arrange — Stripe reports the subscription as PastDue so the sync detects the first payment failure
        StripeState.OverrideSubscriptionStatus = StripeSubscriptionStatus.PastDue;
        SetupSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_failed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var billingEventCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.PaymentFailed) }]
        );
        billingEventCount.Should().Be(1, "a first-payment-failed webhook should append a PaymentFailed row to the log");
    }

    [Fact]
    public async Task AppendBillingEvent_WhenCustomerDeleted_ShouldAppendSuspendedRowWithCustomerDeletedReason()
    {
        // Arrange — Stripe reports the customer as deleted so the sync takes the early-return suspended path
        StripeState.SimulateCustomerDeleted = true;
        SetupSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.deleted");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var billingEventCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionSuspended) }]
        );
        billingEventCount.Should().Be(1, "deleting the Stripe customer should append a SubscriptionSuspended row");

        var suspensionReason = Connection.ExecuteScalar<string>(
            "SELECT suspension_reason FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionSuspended) }]
        );
        suspensionReason.Should().Be("CustomerDeleted", "the row should record why the subscription was suspended");
    }

    [Fact]
    public async Task AppendBillingEvent_WhenSyncRunsTwiceWithIdenticalStripeState_ShouldNotAppendDuplicateRows()
    {
        // Arrange — fresh subscription, deliver a webhook that creates the Stripe subscription
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));

        // Act — first webhook delivery → SubscriptionCreated row appended (along with side-effect rows for the
        // billing-info and payment-method appearance, all detected on the same first sync).
        var firstRequest = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        firstRequest.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed,event_id:evt_first_run");
        var firstResponse = await AnonymousHttpClient.SendAsync(firstRequest);
        firstResponse.EnsureSuccessStatusCode();

        // Act — second webhook delivery for the same logical state (different event_id so it isn't deduped at the
        // acknowledge layer, but no NEW transition of the same kind has occurred). The SubscriptionCreated transition
        // already fired and is captured; running the sync again must not produce a second SubscriptionCreated row.
        // This pins the architectural invariant the append-only design depends on.
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        secondRequest.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated,event_id:evt_second_run");
        var secondResponse = await AnonymousHttpClient.SendAsync(secondRequest);
        secondResponse.EnsureSuccessStatusCode();

        // Assert — exactly one SubscriptionCreated row remains. This is the core no-duplicate property: a transition
        // is recorded once and only once, even when the sync runs multiple times.
        // (We do not assert a stable total-row count across all event types because the MockStripeClient returns a
        // slightly newer CurrentPeriodEnd / state on each call, which can legitimately trigger orthogonal transitions
        // like SubscriptionRenewed on the second run. Asserting total-row count would mask the architectural property
        // we actually care about with mock-specific noise.)
        var subscriptionCreatedCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionCreated) }]
        );
        subscriptionCreatedCount.Should().Be(1, "running the sync twice with identical Stripe state must produce exactly one SubscriptionCreated row, never duplicates");
    }

    [Fact]
    public void BillingEventId_FromComponentsCalledTwiceWithSameInputs_ShouldProduceSameId()
    {
        // Arrange — webhook redelivery after a transaction rollback re-runs detection. The append helper
        // computes the same deterministic ID and silently skips the duplicate insert. This test pins that
        // determinism property at the unit level — the ID is a pure function of its inputs.
        var subscriptionId = SubscriptionId.NewId();
        var occurredAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        const string stripeReference = "sub_abc123";

        // Act
        var firstId = BillingEventId.FromComponents(subscriptionId, BillingEventType.SubscriptionUpgraded, stripeReference, occurredAt);
        var secondId = BillingEventId.FromComponents(subscriptionId, BillingEventType.SubscriptionUpgraded, stripeReference, occurredAt);

        // Assert
        firstId.Should().Be(secondId, "deterministic ID generation must produce equal IDs for equal inputs");
        firstId.Value.Should().StartWith("bilev_", "the ID should carry the BillingEvent prefix");
    }

    [Fact]
    public void BillingEventId_FromComponentsWithDifferentEventType_ShouldProduceDifferentId()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId();
        var occurredAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        const string stripeReference = "sub_abc123";

        // Act
        var upgradedId = BillingEventId.FromComponents(subscriptionId, BillingEventType.SubscriptionUpgraded, stripeReference, occurredAt);
        var downgradedId = BillingEventId.FromComponents(subscriptionId, BillingEventType.SubscriptionDowngraded, stripeReference, occurredAt);

        // Assert
        upgradedId.Should().NotBe(downgradedId, "IDs must differentiate between event types so an upgrade and a downgrade at the same instant produce distinct rows");
    }
}
