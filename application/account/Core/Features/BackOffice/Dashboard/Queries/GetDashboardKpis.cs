using Account.Features.Authentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using Account.Integrations.Stripe;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardKpisQuery(DashboardTrendPeriod Period = DashboardTrendPeriod.Last30Days)
    : IRequest<Result<BackOfficeDashboardKpisResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardKpisResponse(
    DashboardTrendPeriod Period,
    long TotalTenants,
    long ActiveTenants,
    long TrialTenants,
    long CanceledTenants,
    long NewTenantsInPeriod,
    long? NewTenantsDeltaPercent,
    long TotalUsers,
    long ActiveUsersInPeriod,
    decimal BlendedMonthlyRecurringRevenue,
    decimal? BlendedMonthlyRecurringRevenueDeltaPercent,
    decimal TotalRevenue,
    string? Currency,
    long ActiveSessionsLast24Hours
);

public sealed class GetDashboardKpisQueryValidator : AbstractValidator<GetDashboardKpisQuery>
{
    public GetDashboardKpisQueryValidator()
    {
        RuleFor(x => x.Period).Must(p => Enum.IsDefined(typeof(DashboardTrendPeriod), p)).WithMessage("Period must be one of Last7Days, Last30Days, or Last90Days.");
    }
}

public sealed class GetDashboardKpisHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ISubscriptionRepository subscriptionRepository,
    IPlatformCurrencyProvider platformCurrencyProvider,
    TimeProvider timeProvider
) : IRequestHandler<GetDashboardKpisQuery, Result<BackOfficeDashboardKpisResponse>>
{
    // Soft-delete semantic: tenant counts (Total/Active/Trial/Canceled, NewTenantsInPeriod) exclude soft-deleted
    // tenants — a deleted tenant is no longer a tenant. BLENDED MRR sums every active subscription regardless of
    // tenant soft-delete state — subscription/billing rows are immutable historical money facts that outlive the
    // tenant lifecycle, so MRR must not silently drop the moment a paying tenant churns.
    public async Task<Result<BackOfficeDashboardKpisResponse>> Handle(GetDashboardKpisQuery query, CancellationToken cancellationToken)
    {
        var days = DashboardTrendPeriods.GetDays(query.Period);
        var now = timeProvider.GetUtcNow();
        var twentyFourHoursAgo = now.AddHours(-24);
        var periodStart = now.AddDays(-days);
        var priorPeriodStart = now.AddDays(-days * 2);

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var paidSubscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);
        var allUsers = await userRepository.GetAllUnfilteredAsync(cancellationToken);
        var activeSessions = await sessionRepository.CountActiveSinceUnfilteredAsync(twentyFourHoursAgo, cancellationToken);

        // HasEverSubscribed is the same heuristic used by GetTenants/GetTenantsResponse: a successful payment
        // exists in the subscription's payment history. We need it to disambiguate Trial (never paid) from Canceled
        // (was paying, now on free Basis plan). Subscriptions on the free plan are not loaded by GetAllActiveUnfilteredAsync,
        // so look up the full set via tenant ids only when needed.
        var freePlanTenantIds = tenants.Where(t => t.Plan == SubscriptionPlan.Basis).Select(t => t.Id).ToArray();
        var freePlanSubscriptions = freePlanTenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(freePlanTenantIds, cancellationToken);
        var hasEverSubscribedByTenantId = freePlanSubscriptions.ToDictionary(
            s => s.TenantId,
            s => s.PaymentTransactions.Any(t => t.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded)
        );

        var totalTenants = tenants.LongLength;
        var activeTenants = tenants.LongCount(t => t.State == TenantState.Active && t.Plan != SubscriptionPlan.Basis);
        var trialTenants = tenants.LongCount(t =>
            t is { State: TenantState.Active, Plan: SubscriptionPlan.Basis } &&
            !hasEverSubscribedByTenantId.GetValueOrDefault(t.Id)
        );
        var canceledTenants = tenants.LongCount(t =>
            t.Plan == SubscriptionPlan.Basis &&
            hasEverSubscribedByTenantId.GetValueOrDefault(t.Id)
        );

        var newTenantsInPeriod = tenants.LongCount(t => t.CreatedAt >= periodStart);
        var newTenantsInPriorPeriod = tenants.LongCount(t => t.CreatedAt >= priorPeriodStart && t.CreatedAt < periodStart);
        var newTenantsDeltaPercent = newTenantsInPriorPeriod == 0
            ? (long?)null
            : (long)Math.Round((double)(newTenantsInPeriod - newTenantsInPriorPeriod) / newTenantsInPriorPeriod * 100d);

        var activeUsersInPeriod = allUsers.LongCount(u => u.LastSeenAt >= periodStart);

        var totalMonthlyRecurringRevenue = paidSubscriptions.Sum(MrrCalculator.ForwardMrr);

        // Total Revenue: ex-VAT lifetime revenue across every subscription (paid + cancelled). VAT is collected
        // on behalf of tax authorities and never our revenue, so the sum uses AmountExcludingTax. Refunded rows
        // are excluded — money returned to the customer is not revenue.
        var totalRevenue = paidSubscriptions.Concat(freePlanSubscriptions)
            .SelectMany(s => s.PaymentTransactions)
            .Where(t => t.Status == PaymentTransactionStatus.Succeeded)
            .Sum(t => t.AmountExcludingTax);

        // Period-over-period MRR delta uses the MRR contribution of subscriptions that already existed
        // at the start of the period as the prior baseline. The domain does not store historical MRR
        // snapshots, so this approximation treats subscriptions still active today and created before
        // periodStart as the prior-period MRR. Subscriptions created within the period contribute the
        // delta. This is an MRR-driven directional signal rather than a signup-count proxy.
        var priorMonthlyRecurringRevenue = paidSubscriptions.Where(s => s.CreatedAt < periodStart).Sum(MrrCalculator.ForwardMrr);
        var mrrDeltaPercent = priorMonthlyRecurringRevenue == 0m
            ? (decimal?)null
            : Math.Round((totalMonthlyRecurringRevenue - priorMonthlyRecurringRevenue) / priorMonthlyRecurringRevenue * 100m, 1);

        return new BackOfficeDashboardKpisResponse(
            query.Period,
            totalTenants,
            activeTenants,
            trialTenants,
            canceledTenants,
            newTenantsInPeriod,
            newTenantsDeltaPercent,
            allUsers.LongLength,
            activeUsersInPeriod,
            totalMonthlyRecurringRevenue,
            mrrDeltaPercent,
            totalRevenue,
            platformCurrencyProvider.Currency,
            activeSessions
        );
    }
}
