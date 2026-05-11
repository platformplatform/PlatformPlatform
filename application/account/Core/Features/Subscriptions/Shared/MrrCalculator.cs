using Account.Features.Subscriptions.Domain;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Per-subscription forward MRR contribution: 0 if cancelling at period end, the scheduled
///     (downgraded) price if a downgrade is queued, otherwise the current price. Mirrors the
///     per-account MrrAmount tile in the front-end. Used by the dashboard KPI sum and the
///     KPI/trend consistency check — keep them in lockstep by funneling both through this method.
///     ALWAYS returns ex-VAT: MRR is revenue accounting, VAT is collected on behalf of tax authorities
///     and never our revenue, so the KPI sum must be net-of-tax. Both <c>CurrentPriceAmount</c> and
///     <c>ScheduledPriceAmount</c> are stored ex-VAT (normalized at the price-catalog boundary), so
///     this method is a pure read with no tax math.
/// </summary>
public static class MrrCalculator
{
    public static decimal ForwardMrr(Subscription subscription)
    {
        if (!subscription.CurrentPriceAmount.HasValue) return 0m;
        if (subscription.CancelAtPeriodEnd) return 0m;
        return subscription.ScheduledPriceAmount ?? subscription.CurrentPriceAmount.Value;
    }
}
