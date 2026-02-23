using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetPricingCatalogQuery : IRequest<Result<PricingCatalogResponse>>;

[PublicAPI]
public sealed record PricingCatalogResponse(PlanPriceItem[] Plans);

[PublicAPI]
public sealed record PlanPriceItem(SubscriptionPlan Plan, decimal UnitAmount, string Currency, string Interval, int IntervalCount);

public sealed class GetPricingCatalogHandler(StripeClientFactory stripeClientFactory)
    : IRequestHandler<GetPricingCatalogQuery, Result<PricingCatalogResponse>>
{
    public async Task<Result<PricingCatalogResponse>> Handle(GetPricingCatalogQuery query, CancellationToken cancellationToken)
    {
        var catalogItems = await stripeClientFactory.GetClient().GetPriceCatalogAsync(cancellationToken);
        var plans = catalogItems.Select(item => new PlanPriceItem(item.Plan, item.UnitAmount, item.Currency, item.Interval, item.IntervalCount)).ToArray();
        return new PricingCatalogResponse(plans);
    }
}
