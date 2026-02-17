using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetStripeHealthQuery : IRequest<Result<StripeHealthResponse>>;

[PublicAPI]
public sealed record StripeHealthResponse(bool IsConfigured, bool HasPrices);

public sealed class GetStripeHealthHandler(StripeClientFactory stripeClientFactory)
    : IRequestHandler<GetStripeHealthQuery, Result<StripeHealthResponse>>
{
    public async Task<Result<StripeHealthResponse>> Handle(GetStripeHealthQuery query, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var isConfigured = stripeClientFactory.IsConfigured;

        var hasPrices = await stripeClient.GetPriceIdAsync(SubscriptionPlan.Standard, cancellationToken) is not null
                        && await stripeClient.GetPriceIdAsync(SubscriptionPlan.Premium, cancellationToken) is not null;

        return new StripeHealthResponse(isConfigured, hasPrices);
    }
}
