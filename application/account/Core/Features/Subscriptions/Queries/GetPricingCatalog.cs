using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetPricingCatalogQuery : IRequest<Result<PricingCatalogResponse>>;

[PublicAPI]
public sealed record PricingCatalogResponse(PlanPriceItem[] Plans);

[PublicAPI]
public sealed record PlanPriceItem(
    SubscriptionPlan Plan,
    decimal UnitAmount,
    string Currency,
    string Interval,
    int IntervalCount,
    bool TaxInclusive
);

public sealed class GetPricingCatalogHandler(StripeClientFactory stripeClientFactory)
    : IRequestHandler<GetPricingCatalogQuery, Result<PricingCatalogResponse>>
{
    public async Task<Result<PricingCatalogResponse>> Handle(GetPricingCatalogQuery query, CancellationToken cancellationToken)
    {
        var catalogItems = await stripeClientFactory.GetClient().GetPriceCatalogAsync(cancellationToken);
        var plans = catalogItems.Select(item => new PlanPriceItem(item.Plan, item.UnitAmount, item.Currency, item.Interval, item.IntervalCount, item.TaxInclusive)).ToArray();
        return new PricingCatalogResponse(plans);
    }
}
