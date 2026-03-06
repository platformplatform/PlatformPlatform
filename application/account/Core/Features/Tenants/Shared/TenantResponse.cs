using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Domain;

namespace Account.Features.Tenants.Shared;

[PublicAPI]
public sealed record TenantResponse(
    TenantId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    TenantState State,
    SubscriptionPlan Plan,
    SuspensionReason? SuspensionReason,
    Logo Logo
)
{
    public static TenantResponse FromTenant(Tenant tenant)
    {
        return new TenantResponse(
            tenant.Id, tenant.CreatedAt, tenant.ModifiedAt, tenant.Name,
            tenant.State, tenant.Plan, tenant.SuspensionReason, tenant.Logo
        );
    }
}
