using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardPlanDistributionQuery : IRequest<Result<BackOfficeDashboardPlanDistributionResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardPlanDistributionResponse(
    long TotalTenants,
    BackOfficeDashboardPlanDistributionEntry[] Distribution
);

[PublicAPI]
public sealed record BackOfficeDashboardPlanDistributionEntry(SubscriptionPlan Plan, long Count, double Percentage);

public sealed class GetDashboardPlanDistributionHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetDashboardPlanDistributionQuery, Result<BackOfficeDashboardPlanDistributionResponse>>
{
    // Soft-delete semantic: this is a forward-looking current-state snapshot. Soft-deleted tenants are excluded —
    // a deleted tenant has no "current plan" by definition. GetAllUnfilteredAsync bypasses the tenant scope filter
    // but the SoftDelete query filter still applies, so deleted tenants drop out automatically.
    public async Task<Result<BackOfficeDashboardPlanDistributionResponse>> Handle(GetDashboardPlanDistributionQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var totalTenants = tenants.LongLength;

        // Distribution always returns one entry per known plan, even when zero, so the donut renders consistent
        // legend slots regardless of the current data shape.
        var distribution = Enum.GetValues<SubscriptionPlan>()
            .Select(plan =>
                {
                    var count = tenants.LongCount(t => t.Plan == plan);
                    var percentage = totalTenants == 0 ? 0d : Math.Round((double)count / totalTenants * 100d, 1);
                    return new BackOfficeDashboardPlanDistributionEntry(plan, count, percentage);
                }
            )
            .ToArray();

        return new BackOfficeDashboardPlanDistributionResponse(totalTenants, distribution);
    }
}
