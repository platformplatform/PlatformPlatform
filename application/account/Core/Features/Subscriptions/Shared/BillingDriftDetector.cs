using System.Collections.Immutable;
using Account.Features.Subscriptions.Domain;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Pure function that detects drift between the local subscription state and Stripe's authoritative state.
///     Runs inline at the end of every Stripe sync (per-customer) so drift is surfaced immediately on the next
///     webhook for that account, with no scheduled job required.
///     Today the detector covers <see cref="DriftDiscrepancyKind.SubscriptionStateMismatch" /> — comparing
///     `Plan`, `CancelAtPeriodEnd`, `CurrentPriceAmount`, `CurrentPriceCurrency` between the local aggregate
///     and the Stripe snapshot. These fields drive customer access and are operationally the most important
///     to keep aligned. Comparison of stored vs expected BillingEvent rows
///     (<see cref="DriftDiscrepancyKind.MissingEvent" /> / <see cref="DriftDiscrepancyKind.ExtraEvent" /> /
///     <see cref="DriftDiscrepancyKind.FieldDisagree" />) requires a deterministic
///     `ComputeExpectedEvents(StripeSyncSnapshot)` helper that consumes full Stripe history; this is a
///     follow-up extension that plugs into the same return type.
/// </summary>
public static class BillingDriftDetector
{
    public static ImmutableArray<DriftDiscrepancy> Detect(Subscription subscription, StripeSyncSnapshot snapshot)
    {
        var discrepancies = ImmutableArray.CreateBuilder<DriftDiscrepancy>();

        if (subscription.Plan != snapshot.Plan)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Plan differs between local subscription and Stripe.",
                    DriftSeverity.Critical,
                    ExpectedValue: snapshot.Plan.ToString(),
                    ActualValue: subscription.Plan.ToString()
                )
            );
        }

        if (subscription.CancelAtPeriodEnd != snapshot.CancelAtPeriodEnd)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Cancel-at-period-end differs between local subscription and Stripe.",
                    DriftSeverity.Warning,
                    ExpectedValue: snapshot.CancelAtPeriodEnd.ToString(),
                    ActualValue: subscription.CancelAtPeriodEnd.ToString()
                )
            );
        }

        if (subscription.CurrentPriceAmount != snapshot.CurrentPriceAmount)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Current price amount differs between local subscription and Stripe.",
                    DriftSeverity.Critical,
                    ExpectedValue: snapshot.CurrentPriceAmount?.ToString(),
                    ActualValue: subscription.CurrentPriceAmount?.ToString()
                )
            );
        }

        if (subscription.CurrentPriceCurrency != snapshot.CurrentPriceCurrency)
        {
            discrepancies.Add(new DriftDiscrepancy(
                    DriftDiscrepancyKind.SubscriptionStateMismatch,
                    "Current price currency differs between local subscription and Stripe.",
                    DriftSeverity.Warning,
                    ExpectedValue: snapshot.CurrentPriceCurrency,
                    ActualValue: subscription.CurrentPriceCurrency
                )
            );
        }

        return discrepancies.ToImmutable();
    }
}

/// <summary>
///     Snapshot of Stripe's authoritative subscription state captured during a sync. Drives the drift detector
///     and is the seam where additional Stripe data (full invoice history, charge history with refunds,
///     scheduled-phase data) plugs in for the BillingEvent-comparison extension.
/// </summary>
public sealed record StripeSyncSnapshot(
    SubscriptionPlan Plan,
    bool CancelAtPeriodEnd,
    decimal? CurrentPriceAmount,
    string? CurrentPriceCurrency
);
