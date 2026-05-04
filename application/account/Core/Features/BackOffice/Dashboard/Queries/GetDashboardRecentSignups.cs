using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRecentSignupsQuery(int Limit = 6)
    : IRequest<Result<BackOfficeDashboardRecentSignupsResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRecentSignupsResponse(BackOfficeDashboardRecentSignup[] Signups);

[PublicAPI]
public sealed record BackOfficeDashboardRecentSignup(
    TenantId TenantId,
    string Name,
    string? Country,
    SubscriptionPlan Plan,
    string? TenantLogoUrl,
    DateTimeOffset CreatedAt
);

public sealed class GetDashboardRecentSignupsQueryValidator : AbstractValidator<GetDashboardRecentSignupsQuery>
{
    public GetDashboardRecentSignupsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentSignupsHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository
) : IRequestHandler<GetDashboardRecentSignupsQuery, Result<BackOfficeDashboardRecentSignupsResponse>>
{
    public async Task<Result<BackOfficeDashboardRecentSignupsResponse>> Handle(GetDashboardRecentSignupsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetMostRecentSignupsUnfilteredAsync(query.Limit, cancellationToken);
        var tenantIds = tenants.Select(t => t.Id).ToArray();
        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        var signups = tenants.Select(tenant =>
            {
                var subscription = subscriptionByTenantId.GetValueOrDefault(tenant.Id);
                var country = subscription?.BillingInfo?.Address?.Country;
                return new BackOfficeDashboardRecentSignup(
                    tenant.Id,
                    tenant.Name,
                    country,
                    tenant.Plan,
                    tenant.Logo.Url,
                    tenant.CreatedAt
                );
            }
        ).ToArray();

        return new BackOfficeDashboardRecentSignupsResponse(signups);
    }
}
