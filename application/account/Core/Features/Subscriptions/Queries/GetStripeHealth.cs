using JetBrains.Annotations;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetStripeHealthQuery : IRequest<Result<StripeHealthResponse>>;

[PublicAPI]
public sealed record StripeHealthResponse(bool IsConfigured, bool HasApiKey, bool HasWebhookSecret, bool HasStandardPriceId, bool HasPremiumPriceId);

public sealed class GetStripeHealthHandler(StripeClientFactory stripeClientFactory)
    : IRequestHandler<GetStripeHealthQuery, Result<StripeHealthResponse>>
{
    public Task<Result<StripeHealthResponse>> Handle(GetStripeHealthQuery query, CancellationToken cancellationToken)
    {
        var health = stripeClientFactory.GetClient().GetHealth();

        var response = new StripeHealthResponse(
            health.IsConfigured,
            health.HasApiKey,
            health.HasWebhookSecret,
            health.HasStandardPriceId,
            health.HasPremiumPriceId
        );

        return Task.FromResult<Result<StripeHealthResponse>>(response);
    }
}
