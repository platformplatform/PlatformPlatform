using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantDetailQuery(TenantId Id) : IRequest<Result<TenantDetailResponse>>;

[PublicAPI]
public sealed record TenantDetailResponse(
    TenantId Id,
    string Name,
    SubscriptionPlan Plan,
    SubscriptionPlan? ScheduledPlan,
    bool CancelAtPeriodEnd,
    decimal? MonthlyRecurringRevenue,
    string? Currency,
    DateTimeOffset? RenewalDate,
    BillingAddressResponse? BillingAddress,
    decimal? LifetimeValue,
    TenantState State,
    SuspensionReason? SuspensionReason,
    DateTimeOffset? SuspendedAt,
    string? LogoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt
);

[PublicAPI]
public sealed record BillingAddressResponse(
    string? Line1,
    string? Line2,
    string? PostalCode,
    string? City,
    string? State,
    string? Country
);

public sealed class GetTenantDetailHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetTenantDetailQuery, Result<TenantDetailResponse>>
{
    public async Task<Result<TenantDetailResponse>> Handle(GetTenantDetailQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantDetailResponse>.NotFound($"Tenant with id '{query.Id}' was not found.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(tenant.Id, cancellationToken);

        var lifetimeValue = subscription?.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Succeeded)
            .Sum(t => t.Amount);

        var billingAddress = subscription?.BillingInfo?.Address is { } address
            ? new BillingAddressResponse(address.Line1, address.Line2, address.PostalCode, address.City, address.State, address.Country)
            : null;

        return new TenantDetailResponse(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            subscription?.ScheduledPlan,
            subscription?.CancelAtPeriodEnd ?? false,
            subscription?.CurrentPriceAmount,
            subscription?.CurrentPriceCurrency,
            subscription?.CurrentPeriodEnd,
            billingAddress,
            lifetimeValue,
            tenant.State,
            tenant.SuspensionReason,
            tenant.SuspendedAt,
            tenant.Logo.Url,
            tenant.CreatedAt,
            tenant.ModifiedAt
        );
    }
}
