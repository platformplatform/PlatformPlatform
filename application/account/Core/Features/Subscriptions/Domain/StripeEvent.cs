using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("evt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, StripeEventId>))]
public sealed record StripeEventId(string Value) : StronglyTypedString<StripeEventId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
///     A durable archive of every Stripe webhook payload we have observed for this account. Two roles:
///     (1) inbox for two-phase webhook processing (Pending → Processed), and
///     (2) authoritative source for replaying BillingEvents beyond Stripe's 30-day events.list retention
///     (see https://docs.stripe.com/api/events).
///     Rows are immutable after <see cref="MarkProcessed" />. The state machine is one-way:
///     Pending → Processed (or Ignored when no customer match, or Failed on processing error). Subsequent
///     redeliveries of the same event id are deduplicated at insert time and never overwrite an existing row.
///     If the same event id is observed with a different payload, it is logged as a forensic anomaly and the
///     existing row is preserved unchanged.
///     Hard rule: rows in this table are never deleted. Schema changes use ALTER TABLE ADD/DROP COLUMN,
///     never DROP/TRUNCATE/DELETE FROM.
/// </summary>
public sealed class StripeEvent : AggregateRoot<StripeEventId>
{
    private StripeEvent(StripeEventId id) : base(id)
    {
        EventType = string.Empty;
        Status = StripeEventStatus.Pending;
    }

    public string EventType { get; private set; }

    public StripeEventStatus Status { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public StripeCustomerId? StripeCustomerId { get; private set; }

    public StripeSubscriptionId? StripeSubscriptionId { get; private set; }

    public TenantId? TenantId { get; private set; }

    public string? Payload { get; private set; }

    public string? Error { get; private set; }

    /// <summary>
    ///     The Stripe API version active when Stripe created this event. Pinned at event creation time and
    ///     never changes (see https://docs.stripe.com/api/events). The replayer uses this to dispatch to
    ///     the correct <c>IStripeEventPayloadResolver</c> when the JSON shape changes between Stripe API
    ///     versions. Null only on rows recorded before this column existed.
    /// </summary>
    public string? ApiVersion { get; private set; }

    /// <summary>
    ///     When this event was recovered from a reconciliation source (events.list or
    ///     webhook_endpoint_deliveries) instead of arriving via webhook delivery. Null for events that came
    ///     via normal webhook delivery. Forensics: a row with non-null RecoveredAt is a webhook we
    ///     <em>didn't</em> receive in real-time.
    /// </summary>
    public DateTimeOffset? RecoveredAt { get; private set; }

    /// <summary>
    ///     The reconciliation source that recovered this event. <c>"events_list"</c> means we found it via
    ///     Stripe's events.list API; <c>"delivery_audit"</c> means Stripe's webhook_endpoint_deliveries API
    ///     showed us a delivery attempt we never acked. Null when arrived via webhook delivery.
    /// </summary>
    public string? RecoverySource { get; private set; }

    /// <summary>
    ///     SHA-256 hash of the raw payload when this row was first stored. Used by AcknowledgeStripeWebhook
    ///     to detect StripeEventPayloadDivergence: if the same event id arrives twice with different
    ///     payloads, the existing row is preserved unchanged and the divergence is surfaced as a drift
    ///     discrepancy. Null only on rows recorded before this column existed.
    /// </summary>
    public string? PayloadHash { get; private set; }

    /// <summary>
    ///     Stripe's authoritative <c>Event.Created</c> timestamp (see https://docs.stripe.com/api/events).
    ///     Captured at ingestion from both webhook deliveries and reconciliation sources so the replayer
    ///     can order events and stamp <c>BillingEvent.OccurredAt</c> from the time Stripe says the event
    ///     occurred — never our ingestion time. Null only on rows recorded before this column existed;
    ///     the replayer falls back to <c>CreatedAt</c> (ingestion time) in that case.
    /// </summary>
    public DateTimeOffset? StripeCreatedAt { get; private set; }

    /// <summary>
    ///     Factory method for phase 1 webhook acknowledgment. Creates a Pending event that will be
    ///     batch-processed in phase 2. TenantId and StripeSubscriptionId are filled in by phase 2 via
    ///     <see cref="MarkProcessed" />.
    /// </summary>
    public static StripeEvent Create(
        string stripeEventId,
        string eventType,
        StripeCustomerId? stripeCustomerId,
        string? payload,
        string? apiVersion,
        string? payloadHash,
        DateTimeOffset? stripeCreatedAt = null
    )
    {
        return new StripeEvent(StripeEventId.NewId(stripeEventId))
        {
            EventType = eventType,
            StripeCustomerId = stripeCustomerId,
            Payload = payload,
            ApiVersion = apiVersion,
            PayloadHash = payloadHash,
            StripeCreatedAt = stripeCreatedAt
        };
    }

    /// <summary>
    ///     Factory method for events recovered via reconciliation (events.list or webhook_endpoint_deliveries).
    ///     Lands directly as Processed because reconciliation runs inside the same transaction as the replayer,
    ///     and there's no signature to verify (events.list and webhook_endpoint_deliveries are authenticated by
    ///     API key, not webhook signature). The two-phase pending → processed split exists for incoming
    ///     webhooks; recovered events skip phase 1.
    /// </summary>
    public static StripeEvent CreateRecovered(
        string stripeEventId,
        string eventType,
        StripeCustomerId? stripeCustomerId,
        string? payload,
        string? apiVersion,
        string? payloadHash,
        DateTimeOffset recoveredAt,
        string recoverySource,
        DateTimeOffset? stripeCreatedAt = null
    )
    {
        return new StripeEvent(StripeEventId.NewId(stripeEventId))
        {
            EventType = eventType,
            Status = StripeEventStatus.Processed,
            ProcessedAt = recoveredAt,
            StripeCustomerId = stripeCustomerId,
            Payload = payload,
            ApiVersion = apiVersion,
            PayloadHash = payloadHash,
            RecoveredAt = recoveredAt,
            RecoverySource = recoverySource,
            StripeCreatedAt = stripeCreatedAt
        };
    }

    /// <summary>
    ///     Marks the event as successfully processed during phase 2 batch processing. Backfills the
    ///     resolved tenant id and Stripe subscription id at the same moment so the row is fully populated
    ///     before transitioning out of the Pending state. After this call the row is logically immutable —
    ///     no method on this aggregate mutates state once Status is Processed.
    /// </summary>
    public void MarkProcessed(DateTimeOffset processedAt, TenantId? tenantId, StripeSubscriptionId? stripeSubscriptionId)
    {
        EnsurePending();
        Status = StripeEventStatus.Processed;
        ProcessedAt = processedAt;
        TenantId = tenantId;
        StripeSubscriptionId = stripeSubscriptionId;
    }

    /// <summary>
    ///     Marks the event as ignored during phase 1 when no customer ID is present.
    /// </summary>
    public void MarkIgnored(DateTimeOffset processedAt)
    {
        EnsurePending();
        Status = StripeEventStatus.Ignored;
        ProcessedAt = processedAt;
    }

    /// <summary>
    ///     Marks the event as failed with an error message when phase 2 processing encounters an error.
    /// </summary>
    public void MarkFailed(DateTimeOffset failedAt, string error)
    {
        EnsurePending();
        Status = StripeEventStatus.Failed;
        ProcessedAt = failedAt;
        Error = error;
    }

    private void EnsurePending()
    {
        if (Status is not StripeEventStatus.Pending)
        {
            throw new InvalidOperationException($"StripeEvent '{Id.Value}' is no longer Pending (status: {Status}); refusing to mutate.");
        }
    }
}
