using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
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
    decimal? ScheduledPriceAmount,
    bool CancelAtPeriodEnd,
    decimal? MonthlyRecurringRevenue,
    string? Currency,
    DateTimeOffset? RenewalDate,
    DateTimeOffset? SubscribedSince,
    bool HasEverSubscribed,
    string? BillingName,
    string? TaxId,
    BillingAddressResponse? BillingAddress,
    PaymentMethodResponse? PaymentMethod,
    decimal? LifetimeValue,
    TenantState State,
    SuspensionReason? SuspensionReason,
    DateTimeOffset? SuspendedAt,
    string? LogoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    bool HasDriftDetected,
    DateTimeOffset? DriftCheckedAt,
    DriftDiscrepancy[] DriftDiscrepancies,
    string? StripeCustomerUrl
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

[PublicAPI]
public sealed record PaymentMethodResponse(string Brand, string Last4, int ExpMonth, int ExpYear);

public sealed class GetTenantDetailHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository, StripeClientFactory stripeClientFactory)
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
            .Sum(t => t.InvoiceTotal);

        var hasEverSubscribed = subscription?.PaymentTransactions
            .Any(t => t.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded) == true;

        var billingAddress = subscription?.BillingInfo?.Address is { } address
            ? new BillingAddressResponse(address.Line1, address.Line2, address.PostalCode, address.City, address.State, address.Country)
            : null;

        var paymentMethod = subscription?.PaymentMethod is { } currentPaymentMethod
            ? new PaymentMethodResponse(currentPaymentMethod.Brand, currentPaymentMethod.Last4, currentPaymentMethod.ExpMonth, currentPaymentMethod.ExpYear)
            : null;

        return new TenantDetailResponse(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            subscription?.ScheduledPlan,
            subscription?.ScheduledPriceAmount,
            subscription?.CancelAtPeriodEnd ?? false,
            subscription?.CurrentPriceAmount,
            subscription?.CurrentPriceCurrency,
            subscription?.CurrentPeriodEnd,
            subscription?.SubscribedSince,
            hasEverSubscribed,
            subscription?.BillingInfo?.Name,
            subscription?.BillingInfo?.TaxId,
            billingAddress,
            paymentMethod,
            lifetimeValue,
            tenant.State,
            tenant.SuspensionReason,
            tenant.SuspendedAt,
            tenant.Logo.Url,
            tenant.CreatedAt,
            tenant.ModifiedAt,
            subscription?.HasDriftDetected ?? false,
            subscription?.DriftCheckedAt,
            subscription?.DriftDiscrepancies.ToArray() ?? [],
            subscription?.StripeCustomerId is { } stripeCustomerId ? stripeClientFactory.GetClient().BuildCustomerDashboardUrl(stripeCustomerId) : null
        );
    }
}
