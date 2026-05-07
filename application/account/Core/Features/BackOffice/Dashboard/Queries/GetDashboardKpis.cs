using Account.Features.Authentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
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
    string Currency,
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
    TimeProvider timeProvider
) : IRequestHandler<GetDashboardKpisQuery, Result<BackOfficeDashboardKpisResponse>>
{
    // Single supported currency until multi-currency MRR is in scope; matches the existing Subscription DTO style.
    private const string DefaultCurrency = "DKK";

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
            s => s.PaymentTransactions.Any(t => t.Status == PaymentTransactionStatus.Succeeded)
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

        // Period-over-period MRR delta is approximated from the new-tenant signup ratio because the domain does
        // not store historical MRR snapshots. Operators get a directional signal without a daily snapshot table.
        var mrrDeltaPercent = newTenantsInPriorPeriod == 0
            ? (decimal?)null
            : Math.Round(((decimal)newTenantsInPeriod - newTenantsInPriorPeriod) / newTenantsInPriorPeriod * 100m, 1);

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
            DefaultCurrency,
            activeSessions
        );
    }
}
