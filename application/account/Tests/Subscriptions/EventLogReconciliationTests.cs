using System.Globalization;
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
    public async Task Reconcile_WhenPersistedBillingEventHasStaleDenormalizedFields_ShouldFlagDriftAndPreservePersistedRow()
    {
        // Arrange — a customer.subscription.deleted event was classified and persisted while the local
        // state machine was wrong (e.g. an earlier customer.subscription.created webhook had been missed
        // and the row was emitted with CommittedMrr=0). The persisted row therefore carries
        // AmountDelta=0/PreviousAmount=0/NewAmount=0. The events.list view of the world now exposes the
        // created event ahead of the deleted event, so a fresh classification produces a correct deleted
        // row (AmountDelta=-29, PreviousAmount=29), but the append-only invariant forbids mutating the
        // persisted row.
        SetupSubscription();

        var now = TimeProvider.GetUtcNow();
        var createdEventId = "evt_mock_subscription_created_seed";
        var createdOccurredAt = now.AddHours(-2);
        var createdPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard"}}]}}}}""";

        var deletedEventId = "evt_mock_subscription_deleted_seed";
        var deletedOccurredAt = now.AddMinutes(-30);
        var deletedPayload = """{"data":{"object":{"status":"canceled","cancel_at_period_end":false}}}""";

        // Stripe's events.list returns the historical created+deleted pair on the next sync. The hot path
        // emitter drives BillingEvent classification straight from this response (never the local archive),
        // so the deleted row's fresh denormalized fields will not match the previously-persisted row.
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(createdEventId, "customer.subscription.created", createdOccurredAt, createdPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(deletedEventId, "customer.subscription.deleted", deletedOccurredAt, deletedPayload, MockStripeClient.MockApiVersion));

        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", deletedOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", deletedEventId),
                ("event_type", nameof(BillingEventType.SubscriptionImmediatelyCancelled)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Basis)),
                ("previous_amount", 0m),
                ("new_amount", 0m),
                ("amount_delta", 0m),
                ("committed_mrr", 0m),
                ("currency", "DKK"),
                ("occurred_at", deletedOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // Act — any subsequent webhook triggers ProcessPendingStripeEvents, which now consumes the
        // events.list view of the world. The classifier reads the created+deleted pair and produces
        // deleted-row denormalized values that differ from what is persisted.
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert — append-only invariant: the persisted row's denormalized fields are untouched.
        response.EnsureSuccessStatusCode();

        var persistedAmountDelta = Connection.ExecuteScalar<string>(
            "SELECT amount_delta FROM billing_events WHERE stripe_event_id = @id", [new { id = deletedEventId }]
        );
        decimal.Parse(persistedAmountDelta, CultureInfo.InvariantCulture).Should().Be(0m, "the append-only invariant forbids mutating the persisted billing_event row even when classification would produce different values");

        // Assert — drift surfaces the staleness for operator review.
        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            "SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        driftDiscrepanciesJson.Should().Contain(
            nameof(DriftDiscrepancyKind.BillingEventDenormalizationStale),
            "the persisted deleted row's denormalized fields no longer match a fresh classification and must be surfaced as drift"
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

    [Fact]
    public async Task Reconcile_WhenEventsListReturnsEventsWithOlderStripeCreated_ShouldStampBillingEventOccurredAtFromStripeCreated()
    {
        // Arrange — two recovered events whose Stripe Event.Created is hours older than `now`. The replayer
        // must source BillingEvent.OccurredAt from the Stripe-authoritative timestamp surfaced via
        // StripeReplayEvent.CreatedAt (StripeEvent.StripeCreatedAt ?? CreatedAt) so dashboards surface the
        // event at the moment Stripe says it occurred, not at our ingestion time.
        SetupSubscription();

        var now = TimeProvider.GetUtcNow();
        var createdEventId = "evt_stripe_created_subscription_created";
        var createdStripeCreated = now.AddHours(-3);
        var createdPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard"}}]}}}}""";

        var renewedEventId = "evt_stripe_created_invoice_payment_succeeded";
        var renewedStripeCreated = now.AddHours(-1);
        var renewedPayload = """{"data":{"object":{"attempt_count":1,"billing_reason":"subscription_cycle"}}}""";

        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(createdEventId, "customer.subscription.created", createdStripeCreated, createdPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(renewedEventId, "invoice.payment_succeeded", renewedStripeCreated, renewedPayload, MockStripeClient.MockApiVersion));

        // Act — webhook arrives, triggers ProcessPendingStripeEvents which runs reconciliation and replay.
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert — BillingEvent.OccurredAt matches Stripe's Created for both recovered events, not `now`.
        response.EnsureSuccessStatusCode();

        var createdOccurredAt = ParseTimestamp(Connection.ExecuteScalar<string>(
                "SELECT occurred_at FROM billing_events WHERE stripe_event_id = @id", [new { id = createdEventId }]
            )
        );
        createdOccurredAt.Should().BeCloseTo(createdStripeCreated, TimeSpan.FromSeconds(1), "OccurredAt must reflect Stripe Event.Created, not our ingestion time");

        var renewedOccurredAt = ParseTimestamp(Connection.ExecuteScalar<string>(
                "SELECT occurred_at FROM billing_events WHERE stripe_event_id = @id", [new { id = renewedEventId }]
            )
        );
        renewedOccurredAt.Should().BeCloseTo(renewedStripeCreated, TimeSpan.FromSeconds(1), "OccurredAt must reflect Stripe Event.Created, not our ingestion time");

        // Assert — the recovered stripe_events rows carry stripe_created_at sourced from events.list.
        var createdStripeCreatedAtPersisted = ParseTimestamp(Connection.ExecuteScalar<string>(
                "SELECT stripe_created_at FROM stripe_events WHERE id = @id", [new { id = createdEventId }]
            )
        );
        createdStripeCreatedAtPersisted.Should().BeCloseTo(createdStripeCreated, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AcknowledgeWebhook_WhenWebhookDeliveryArrives_ShouldPersistStripeCreatedAtFromWebhookEvent()
    {
        // Arrange — a webhook delivery's Stripe Event.Created timestamp must be threaded from
        // StripeWebhookEventResult.Created into StripeEvent.StripeCreatedAt so the replayer can later order
        // events and write BillingEvent.OccurredAt from Stripe's authoritative time rather than ingestion
        // time. MockStripeClient.VerifyWebhookSignature sources Created from the same TimeProvider the
        // production handler injects, so capturing the window around the webhook call brackets the
        // persisted timestamp.
        SetupSubscription();

        var eventId = $"evt_stripe_created_webhook_{Guid.NewGuid():N}";
        var before = TimeProvider.GetUtcNow();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:customer.subscription.updated,event_id:{eventId}");
        var response = await AnonymousHttpClient.SendAsync(request);
        var after = TimeProvider.GetUtcNow();

        // Assert — the persisted row's stripe_created_at column matches the webhook event's Stripe Created
        // timestamp (within the request window), proving the field is populated from
        // StripeWebhookEventResult.Created rather than left null.
        response.EnsureSuccessStatusCode();

        var persistedStripeCreatedAt = ParseTimestamp(Connection.ExecuteScalar<string>(
                "SELECT stripe_created_at FROM stripe_events WHERE id = @id", [new { id = eventId }]
            )
        );
        persistedStripeCreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        persistedStripeCreatedAt.Should().BeOnOrBefore(after.AddSeconds(1));
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
