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
                ("current_price_amount", hasStripeSubscription ? MockStripeClient.StandardAmountExcludingTax : null),
                ("current_price_currency", hasStripeSubscription ? MockStripeClient.MockStandardCurrency : null),
                ("current_period_end", hasStripeSubscription ? TimeProvider.GetUtcNow().AddDays(30) : null)
            ]
        );
    }

    [Fact]
    public async Task Reconcile_WhenPersistedBillingEventHasStaleDenormalizedFields_ShouldFlagDriftAndPreservePersistedRow()
    {
        // a customer.subscription.deleted event was classified and persisted while the local
        // state machine was wrong (e.g. an earlier customer.subscription.created webhook had been missed
        // and the row was emitted with CommittedMrr=0). The persisted row therefore carries
        // AmountDelta=0/PreviousAmount=0/NewAmount=0. The events.list view of the world now exposes the
        // created event ahead of the deleted event, so a fresh classification produces a correct deleted
        // row (AmountDelta=-29, PreviousAmount=29), but the append-only invariant forbids mutating the
        // persisted row.
        //
        // The staleness loop skips the seed-boundary row (the latest persisted) by design — see the
        // comment in ProcessPendingStripeEvents around the staleness loop — so this test pins a newer
        // BillingInfoUpdated row after the deleted to keep the deleted comparable for the staleness
        // detector. Without it the deleted would BE the seed boundary and its mismatch would be
        // legitimately suppressed.
        // Arrange
        SetupSubscription();

        var now = TimeProvider.GetUtcNow();
        var createdEventId = "evt_mock_subscription_created_seed";
        var createdOccurredAt = now.AddHours(-2);
        var createdPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard"}}]}}}}""";

        var deletedEventId = "evt_mock_subscription_deleted_seed";
        var deletedOccurredAt = now.AddMinutes(-30);
        var deletedPayload = """{"data":{"object":{"status":"canceled","cancel_at_period_end":false}}}""";

        var billingInfoEventId = "evt_mock_billing_info_updated_after_deleted";
        var billingInfoOccurredAt = now.AddMinutes(-5);
        var billingInfoPayload = """{"data":{"object":{"name":"Acme Inc"}}}""";

        // Stripe's events.list returns the historical created+deleted pair on the next sync. The hot path
        // emitter drives BillingEvent classification straight from this response (never the local archive),
        // so the deleted row's fresh denormalized fields will not match the previously-persisted row.
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(createdEventId, "customer.subscription.created", createdOccurredAt, createdPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(deletedEventId, "customer.subscription.deleted", deletedOccurredAt, deletedPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(billingInfoEventId, "customer.updated", billingInfoOccurredAt, billingInfoPayload, MockStripeClient.MockApiVersion));

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
                ("currency", MockStripeClient.MockStandardCurrency),
                ("occurred_at", deletedOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", billingInfoOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", billingInfoEventId),
                ("event_type", nameof(BillingEventType.BillingInfoUpdated)),
                ("from_plan", null),
                ("to_plan", null),
                ("previous_amount", null),
                ("new_amount", null),
                ("amount_delta", null),
                ("committed_mrr", 0m),
                ("currency", null),
                ("occurred_at", billingInfoOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // any subsequent webhook triggers ProcessPendingStripeEvents, which now consumes the
        // events.list view of the world. The classifier reads the created+deleted pair and produces
        // deleted-row denormalized values that differ from what is persisted.
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // append-only invariant: the persisted row's denormalized fields are untouched.
        // Assert
        response.EnsureSuccessStatusCode();

        var persistedAmountDelta = Connection.ExecuteScalar<string>(
            "SELECT amount_delta FROM billing_events WHERE stripe_event_id = @id", [new { id = deletedEventId }]
        );
        decimal.Parse(persistedAmountDelta, CultureInfo.InvariantCulture).Should().Be(0m, "the append-only invariant forbids mutating the persisted billing_event row even when classification would produce different values");

        // drift surfaces the staleness for operator review.
        // Assert
        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            "SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        driftDiscrepanciesJson.Should().Contain(
            nameof(DriftDiscrepancyKind.BillingEventDenormalizationStale),
            "the persisted deleted row's denormalized fields no longer match a fresh classification and must be surfaced as drift"
        );
    }

    [Fact]
    public async Task Reconcile_WhenLatestPersistedRowIsBoundaryAndReReplays_ShouldNotEmitFalsePositiveStaleDrift()
    {
        // Stripe's events.list?created.gte=X is inclusive on X. After Apply mode advances the anchor to
        // supportedEvents.Max(CreatedAt), the very next sync re-pulls the boundary event. Replay seeds
        // from that event's persisted denormalized fields, then re-runs the event from a state that IS
        // its own output — by construction the per-event previousMrr / amountDelta / newAmount come out
        // different from what's persisted, even when the persisted row is correct. Without the boundary
        // skip the staleness loop fires BillingEventDenormalizationStale on every sync forever, neither
        // Reconcile nor Acknowledge can clear it, and the back-office offers disaster recovery for an
        // in-window customer where it's structurally pointless.
        // Arrange
        SetupSubscription();

        var now = TimeProvider.GetUtcNow();
        var upgradedEventId = "evt_mock_boundary_upgraded";
        var upgradedOccurredAt = now.AddDays(-1);
        var upgradedPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_premium"}}]}}}}""";

        // events.list returns the boundary event again on this sync (inclusive cursor) — same id, same
        // CreatedAt — exactly as Stripe's API does in steady state.
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(upgradedEventId, "customer.subscription.updated", upgradedOccurredAt, upgradedPayload, MockStripeClient.MockApiVersion));

        // The persisted row carries the correct denormalized fields a prior sync produced.
        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", upgradedOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", upgradedEventId),
                ("event_type", nameof(BillingEventType.SubscriptionUpgraded)),
                ("from_plan", nameof(SubscriptionPlan.Standard)),
                ("to_plan", nameof(SubscriptionPlan.Premium)),
                ("previous_amount", MockStripeClient.StandardAmountExcludingTax),
                ("new_amount", MockStripeClient.PremiumAmountExcludingTax),
                ("amount_delta", MockStripeClient.PremiumAmountExcludingTax - MockStripeClient.StandardAmountExcludingTax),
                ("committed_mrr", MockStripeClient.PremiumAmountExcludingTax),
                ("currency", MockStripeClient.MockStandardCurrency),
                ("occurred_at", upgradedOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            "SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        driftDiscrepanciesJson.Should().NotContain(
            nameof(DriftDiscrepancyKind.BillingEventDenormalizationStale),
            "the boundary row's mismatch is a structural artifact of self-seeding and must not fire drift"
        );

        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(0, "Reconcile must clear the drift flag for an in-window customer with correct persisted denormalization");
    }

    [Fact]
    public async Task Reconcile_WhenLatestPersistedRowHasSameSecondSiblings_ShouldNotEmitFalsePositiveStaleDriftForSiblings()
    {
        // Stripe routinely emits multiple events at the same `created` timestamp. A Standard-to-Premium
        // upgrade emits three sibling events at the same second: a per-plan NoOp before, the
        // SubscriptionUpgraded itself, and a per-plan NoOp after. The boundary skip picks ONE of those as
        // the seed source; re-replaying any of its siblings from a state seeded by that pick produces
        // structurally different denormalized fields by construction. Suppressing only the seed row by
        // event id would leave sibling false-positives firing forever — exactly the scenario we observed
        // on `cus_UO5zFt8uzYpDm7`. The staleness loop must skip the entire same-second cluster.
        // Arrange
        SetupSubscription();

        var now = TimeProvider.GetUtcNow();
        var clusterOccurredAt = now.AddDays(-1);
        var preNoOpEventId = "evt_mock_cluster_noop_pre";
        var upgradedEventId = "evt_mock_cluster_upgraded";
        var postNoOpEventId = "evt_mock_cluster_noop_post";
        var noOpPayload = """{"data":{"object":{"name":"Acme Inc"}}}""";
        var upgradedPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_premium"}}]}}}}""";

        // events.list returns all three siblings at the same Stripe Created — exactly how Stripe presents
        // an upgrade flow in steady state.
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(preNoOpEventId, "customer.updated", clusterOccurredAt, noOpPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(upgradedEventId, "customer.subscription.updated", clusterOccurredAt, upgradedPayload, MockStripeClient.MockApiVersion));
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(postNoOpEventId, "customer.updated", clusterOccurredAt, noOpPayload, MockStripeClient.MockApiVersion));

        // Persisted rows for the three siblings. CommittedMrr values mirror the original persistence
        // ordering. The pre-NoOp carries the pre-upgrade total; the Upgraded carries the actual delta
        // math; the post-NoOp carries the post-upgrade total. Re-replaying from a seed at any single
        // sibling produces different values for the others by construction.
        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", clusterOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", preNoOpEventId),
                ("event_type", nameof(BillingEventType.BillingInfoUpdated)),
                ("from_plan", null),
                ("to_plan", null),
                ("previous_amount", null),
                ("new_amount", null),
                ("amount_delta", null),
                ("committed_mrr", MockStripeClient.StandardAmountExcludingTax),
                ("currency", null),
                ("occurred_at", clusterOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", clusterOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", upgradedEventId),
                ("event_type", nameof(BillingEventType.SubscriptionUpgraded)),
                ("from_plan", nameof(SubscriptionPlan.Standard)),
                ("to_plan", nameof(SubscriptionPlan.Premium)),
                ("previous_amount", MockStripeClient.StandardAmountExcludingTax),
                ("new_amount", MockStripeClient.PremiumAmountExcludingTax),
                ("amount_delta", MockStripeClient.PremiumAmountExcludingTax - MockStripeClient.StandardAmountExcludingTax),
                ("committed_mrr", MockStripeClient.PremiumAmountExcludingTax),
                ("currency", MockStripeClient.MockStandardCurrency),
                ("occurred_at", clusterOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", DatabaseSeeder.Tenant1Subscription.Id.Value),
                ("created_at", clusterOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", postNoOpEventId),
                ("event_type", nameof(BillingEventType.BillingInfoUpdated)),
                ("from_plan", null),
                ("to_plan", null),
                ("previous_amount", null),
                ("new_amount", null),
                ("amount_delta", null),
                ("committed_mrr", MockStripeClient.PremiumAmountExcludingTax),
                ("currency", null),
                ("occurred_at", clusterOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            "SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        driftDiscrepanciesJson.Should().NotContain(
            nameof(DriftDiscrepancyKind.BillingEventDenormalizationStale),
            "same-second sibling rows mismatch under self-seeded replay by construction and must not fire drift"
        );

        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(0, "Reconcile must clear the drift flag for an in-window customer whose only mismatches are structural same-second-sibling artifacts");
    }

    [Fact]
    public async Task Reconcile_WhenEventsListReturnsEventNotInLocalArchive_ShouldInsertAsRecovered()
    {
        // fresh subscription with no recorded events. The MockStripeClient.GetEventsForCustomerAsync
        // returns a default customer.subscription.created event (id = MockStripeClient.MockSubscriptionCreatedEventId).
        // Arrange
        SetupSubscription(null, nameof(SubscriptionPlan.Basis));

        // webhook arrives, triggers ProcessPendingStripeEvents which runs reconciliation
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // the missing customer.subscription.created event should be inserted as recovered
        // Assert
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
        // two recovered events whose Stripe Event.Created is hours older than `now`. The replayer
        // must source BillingEvent.OccurredAt from the Stripe-authoritative timestamp surfaced via
        // StripeReplayEvent.CreatedAt (StripeEvent.StripeCreatedAt ?? CreatedAt) so dashboards surface the
        // event at the moment Stripe says it occurred, not at our ingestion time.
        // Arrange
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

        // webhook arrives, triggers ProcessPendingStripeEvents which runs reconciliation and replay.
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // BillingEvent.OccurredAt matches Stripe's Created for both recovered events, not `now`.
        // Assert
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

        // the recovered stripe_events rows carry stripe_created_at sourced from events.list.
        // Assert
        var createdStripeCreatedAtPersisted = ParseTimestamp(Connection.ExecuteScalar<string>(
                "SELECT stripe_created_at FROM stripe_events WHERE id = @id", [new { id = createdEventId }]
            )
        );
        createdStripeCreatedAtPersisted.Should().BeCloseTo(createdStripeCreated, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AcknowledgeWebhook_WhenWebhookDeliveryArrives_ShouldPersistStripeCreatedAtFromWebhookEvent()
    {
        // a webhook delivery's Stripe Event.Created timestamp must be threaded from
        // StripeWebhookEventResult.Created into StripeEvent.StripeCreatedAt so the replayer can later order
        // events and write BillingEvent.OccurredAt from Stripe's authoritative time rather than ingestion
        // time. MockStripeClient.VerifyWebhookSignature sources Created from the same TimeProvider the
        // production handler injects, so capturing the window around the webhook call brackets the
        // persisted timestamp.
        // Arrange
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

        // the persisted row's stripe_created_at column matches the webhook event's Stripe Created
        // timestamp (within the request window), proving the field is populated from
        // StripeWebhookEventResult.Created rather than left null.
        // Assert
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
