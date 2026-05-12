using System.Collections.Immutable;
using Account.Features.Subscriptions.Domain;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Pure function that detects drift between the local subscription state and Stripe's authoritative state.
///     Runs inline at the end of every Stripe sync (per-customer) so drift is surfaced immediately on the next
///     webhook for that account, with no scheduled job required.
///     The detector covers <see cref="DriftDiscrepancyKind.SubscriptionStateMismatch" /> — comparing
///     `Plan`, `CancelAtPeriodEnd`, `CurrentPriceAmount`, `CurrentPriceCurrency` between the local snapshot
///     captured before sync mutations and the Stripe snapshot captured from Stripe's response. These fields
///     drive customer access and are operationally the most important to keep aligned. It also flags a coarse
///     <see cref="DriftDiscrepancyKind.MissingEvent" /> when there are stored PaymentTransactions but zero
///     BillingEvent rows for the subscription — invoices made it to the local PaymentTransactions array
///     without a corresponding event row, indicating a bug in the event-emission pipeline. Per-event
///     comparison (<see cref="DriftDiscrepancyKind.ExtraEvent" /> / <see cref="DriftDiscrepancyKind.FieldDisagree" />)
///     requires a deterministic `ComputeExpectedEvents(StripeSyncSnapshot)` helper that consumes full Stripe
///     history; this is a follow-up extension that plugs into the same return type.
/// </summary>
public static class BillingDriftDetector
{
    public static ImmutableArray<DriftDiscrepancy> Detect(StripeSyncSnapshot localSnapshot, StripeSyncSnapshot stripeSnapshot, int paymentTransactionCount, int billingEventCount)
    {
        var discrepancies = ImmutableArray.CreateBuilder<DriftDiscrepancy>();

        // PaymentTransactions exist on the subscription but the BillingEvent log is empty — the event-emission
        // pipeline missed at least one invoice.payment_succeeded. Reconcile is the natural recovery path.
        if (paymentTransactionCount > 0 && billingEventCount == 0)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.MissingEvent,
                    $"Subscription has {paymentTransactionCount} payment transactions but no billing events recorded.",
                    DriftSeverity.Warning,
                    ExpectedValue: paymentTransactionCount.ToString(),
                    ActualValue: "0"
                )
            );
        }

        if (localSnapshot.Plan != stripeSnapshot.Plan)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Plan differs between local subscription and Stripe.",
                    DriftSeverity.Critical,
                    ExpectedValue: stripeSnapshot.Plan.ToString(),
                    ActualValue: localSnapshot.Plan.ToString()
                )
            );
        }

        if (localSnapshot.CancelAtPeriodEnd != stripeSnapshot.CancelAtPeriodEnd)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Cancel-at-period-end differs between local subscription and Stripe.",
                    DriftSeverity.Warning,
                    ExpectedValue: stripeSnapshot.CancelAtPeriodEnd.ToString(),
                    ActualValue: localSnapshot.CancelAtPeriodEnd.ToString()
                )
            );
        }

        if (localSnapshot.CurrentPriceAmount != stripeSnapshot.CurrentPriceAmount)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Current price amount differs between local subscription and Stripe.",
                    DriftSeverity.Critical,
                    ExpectedValue: stripeSnapshot.CurrentPriceAmount?.ToString(),
                    ActualValue: localSnapshot.CurrentPriceAmount?.ToString()
                )
            );
        }

        if (localSnapshot.CurrentPriceCurrency != stripeSnapshot.CurrentPriceCurrency)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Current price currency differs between local subscription and Stripe.",
                    DriftSeverity.Warning,
                    ExpectedValue: stripeSnapshot.CurrentPriceCurrency,
                    ActualValue: localSnapshot.CurrentPriceCurrency
                )
            );
        }

        // ScheduledPlan without ScheduledPriceAmount distorts the BLENDED MRR KPI: MrrCalculator.ForwardMrr
        // falls back from the missing scheduled price to the current (higher) price, overstating forward MRR.
        // The unconditional reconciliation pass in SyncStateFromStripe prevents this from being written;
        // this check stands as defence-in-depth so any future regression surfaces on the next sync.
        if (localSnapshot.ScheduledPlan is not null && localSnapshot.ScheduledPriceAmount is null)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.ScheduledPriceMissing,
                    "Subscription has a scheduled plan but the scheduled price amount is missing. The MRR KPI falls back to the current price instead, distorting BLENDED MRR.",
                    DriftSeverity.Critical,
                    ExpectedValue: localSnapshot.ScheduledPlan.ToString()
                )
            );
        }

        return discrepancies.ToImmutable();
    }
}

/// <summary>
///     Snapshot of subscription state captured at a point in time. Used twice during drift detection: once for
///     the local subscription state captured before any sync mutations are applied, and once for Stripe's
///     authoritative view captured from the SubscriptionSyncResult returned by the Stripe client. Comparing
///     the two surfaces real drift even though the local subscription is mutated to match Stripe later in the
///     same sync. The shape is also the seam where additional Stripe data (full invoice history, charge
///     history with refunds, scheduled-phase data) plugs in for the BillingEvent-comparison extension.
/// </summary>
public sealed record StripeSyncSnapshot(
    SubscriptionPlan Plan,
    bool CancelAtPeriodEnd,
    decimal? CurrentPriceAmount,
    string? CurrentPriceCurrency,
    SubscriptionPlan? ScheduledPlan = null,
    decimal? ScheduledPriceAmount = null
)
{
    public static StripeSyncSnapshot FromSubscription(Subscription subscription)
    {
        return new StripeSyncSnapshot(
            subscription.Plan,
            subscription.CancelAtPeriodEnd,
            subscription.CurrentPriceAmount,
            subscription.CurrentPriceCurrency,
            subscription.ScheduledPlan,
            subscription.ScheduledPriceAmount
        );
    }
}
