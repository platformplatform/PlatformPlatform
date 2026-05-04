using Account.Features.Authentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardKpisQuery : IRequest<Result<BackOfficeDashboardKpisResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardKpisResponse(
    long TotalTenants,
    long ActiveTenants,
    long TrialTenants,
    long CanceledTenants,
    long TotalUsers,
    decimal TotalMonthlyRecurringRevenue,
    string Currency,
    long ActiveSessionsLast24Hours,
    long NewTenantsLast30Days,
    long NewUsersLast30Days
);

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
        var now = timeProvider.GetUtcNow();
        var twentyFourHoursAgo = now.AddHours(-24);
        var thirtyDaysAgo = now.AddDays(-30);

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var subscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);
        var totalUsers = await userRepository.CountAllUnfilteredAsync(cancellationToken);
        var activeSessions = await sessionRepository.CountActiveSinceUnfilteredAsync(twentyFourHoursAgo, cancellationToken);
        var newTenants = await tenantRepository.GetCreatedSinceUnfilteredAsync(thirtyDaysAgo, cancellationToken);
        var newUsers = await userRepository.GetCreatedSinceUnfilteredAsync(thirtyDaysAgo, cancellationToken);

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

        var totalMonthlyRecurringRevenue = subscriptions
            .Where(s => s.CurrentPriceAmount.HasValue)
            .Sum(s => s.CurrentPriceAmount!.Value);

        return new BackOfficeDashboardKpisResponse(
            totalTenants,
            activeTenants,
            trialTenants,
            canceledTenants,
            totalUsers,
            totalMonthlyRecurringRevenue,
            DefaultCurrency,
            activeSessions,
            newTenants.LongLength,
            newUsers.LongLength
        );
    }
}
