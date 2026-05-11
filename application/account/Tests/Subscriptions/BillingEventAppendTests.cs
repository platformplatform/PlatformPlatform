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
                ("current_price_currency", hasStripeSubscription ? "DKK" : null),
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

        // Act — pin the webhook event id to the same id the events.list mock returns so the
        // pending and recovered sources represent the same Stripe event (deduped by the in-memory
        // union; otherwise both sources would produce a SubscriptionSuspended row, since both
        // legitimately classify customer.deleted into a billing event).
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:customer.deleted,event_id:{MockStripeClient.MockCustomerDeletedEventId}");
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
    public async Task AppendBillingEvent_WhenSyncReplaysSameStripeEventTwice_ShouldNotAppendDuplicateRow()
    {
        // Arrange — strict 1:1 invariant: each Stripe event maps to exactly one billing_events row.
        // The unique stripe_event_id index makes redelivered webhooks idempotent.
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        firstRequest.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed,event_id:evt_redelivered");
        var firstResponse = await AnonymousHttpClient.SendAsync(firstRequest);
        firstResponse.EnsureSuccessStatusCode();

        var firstCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );

        // Act — redeliver the SAME logical webhook (same event_id). The replayer reads the existing
        // stripe_events row, but every billing_event whose stripe_event_id is already recorded is skipped.
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        secondRequest.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed,event_id:evt_redelivered");
        var secondResponse = await AnonymousHttpClient.SendAsync(secondRequest);
        secondResponse.EnsureSuccessStatusCode();

        // Assert — total billing_events row count unchanged after redelivery.
        var secondCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        secondCount.Should().Be(firstCount, "redelivering the same Stripe event must not append a duplicate billing_event row");
    }

    [Fact]
    public async Task AppendBillingEvent_WhenJustArrivedWebhookIsNotInEventsListMock_ShouldStillAppendBillingEventInSamePass()
    {
        // Arrange — regression for the multi-source reconciliation bug. The just-arrived webhook is stored
        // as Pending by AcknowledgeStripeWebhook, then ProcessPendingStripeEvents must include it in the same
        // replay pass. Before the fix, the archive query filtered Status=Processed (skipping the just-arrived
        // Pending event) and the events.list reconciliation skipped it too because it was already in
        // stripe_events — resulting in NO billing_event row for the most recent webhook until the next
        // webhook (or admin Sync) ran. The fix unions the in-memory pending events into the replay input.
        SetupSubscription();
        TelemetryEventsCollectorSpy.Reset();
        var uniqueEventId = $"evt_test_unique_{Guid.NewGuid():N}";

        // The MockStripeClient.GetEventsForCustomerAsync mock does NOT contain this event id — exactly the
        // scenario that exposed the bug. The payload is a meaningful classifying customer.subscription.updated
        // event with status active → past_due so the replayer emits a SubscriptionPastDue BillingEvent.
        var payload = """{"data":{"object":{"status":"past_due"},"previous_attributes":{"status":"active"}}}""";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:customer.subscription.updated,event_id:{uniqueEventId}");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert — exactly one billing_events row exists with the just-arrived event's stripe_event_id
        response.EnsureSuccessStatusCode();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND stripe_event_id = @stripeEventId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, stripeEventId = uniqueEventId }]
        );
        rowCount.Should().Be(1, "the just-arrived webhook event must produce a billing_events row in the same processing pass, even when it is not in the events.list reconciliation source");

        var eventType = Connection.ExecuteScalar<string>(
            "SELECT event_type FROM billing_events WHERE tenant_id = @tenantId AND stripe_event_id = @stripeEventId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, stripeEventId = uniqueEventId }]
        );
        eventType.Should().Be(nameof(BillingEventType.SubscriptionPastDue), "the active → past_due payload should classify as SubscriptionPastDue");
    }

    [Fact]
    public async Task DetectDrift_WhenJustArrivedWebhookSatisfiesCoverageInSamePass_ShouldNotFireMissingEventDrift()
    {
        // Arrange — regression for the same-pass coverage hazard. CheckResourceCoverage previously re-queried
        // the local stripe_events archive (Status=Processed only), which excluded events that arrived in this
        // same pass and are still Pending until the UnitOfWork commits. The result was a spurious
        // MissingHistoricalEvent discrepancy on the very webhook that introduced the resource (e.g. a downgrade
        // schedule, or a brand-new subscription). The fix passes the in-memory union of archive + pending +
        // recovered event types from EmitBillingEventsFromEventsListAsync into the coverage check.
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));
        TelemetryEventsCollectorSpy.Reset();
        var uniqueEventId = $"evt_test_unique_{Guid.NewGuid():N}";

        // Act — send the same customer.subscription.created event that the coverage check looks for. After
        // the webhook flow, subscription.SubscribedSince is populated (Basis → Standard transition), which is
        // the trigger condition for the "customer.subscription.created" coverage check.
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:customer.subscription.created,event_id:{uniqueEventId}");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        // The just-arrived event must produce a SubscriptionCreated billing_events row.
        var billingEventCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND stripe_event_id = @stripeEventId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, stripeEventId = uniqueEventId }]
        );
        billingEventCount.Should().Be(1, "the customer.subscription.created event must produce a billing_events row");

        // Drift must not include a coverage discrepancy for the very event we just processed. Before the fix,
        // CheckResourceCoverage saw an empty Processed archive and raised "no customer.subscription.created
        // event is recorded" against the just-arrived (Pending) event.
        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            $"SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = {DatabaseSeeder.Tenant1.Id.Value}",
            []
        );
        driftDiscrepanciesJson.Should().NotContain(
            "customer.subscription.created",
            "the same-pass union must include the just-arrived Pending event so coverage does not flag it as missing"
        );
    }
}
