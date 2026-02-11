using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetCurrentSubscriptionQuery : IRequest<Result<SubscriptionResponse>>;

[PublicAPI]
public sealed record SubscriptionResponse(
    SubscriptionId Id,
    SubscriptionPlan Plan,
    SubscriptionPlan? ScheduledPlan,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    bool HasStripeSubscription,
    PaymentMethod? PaymentMethod
);

public sealed class GetCurrentSubscriptionHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetCurrentSubscriptionQuery, Result<SubscriptionResponse>>
{
    public async Task<Result<SubscriptionResponse>> Handle(GetCurrentSubscriptionQuery query, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            return Result<SubscriptionResponse>.NotFound("Subscription not found for current tenant.");
        }

        return new SubscriptionResponse(
            subscription.Id,
            subscription.Plan,
            subscription.ScheduledPlan,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.StripeSubscriptionId is not null,
            subscription.PaymentMethod
        );
    }
}
