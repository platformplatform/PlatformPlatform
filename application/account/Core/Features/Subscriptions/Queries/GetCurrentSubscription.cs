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
    bool HasStripeCustomer,
    bool HasStripeSubscription,
    PaymentMethod? PaymentMethod,
    BillingInfo? BillingInfo,
    bool IsPaymentFailed,
    bool HasPendingStripeEvents
);

public sealed class GetCurrentSubscriptionHandler(ISubscriptionRepository subscriptionRepository, IStripeEventRepository stripeEventRepository)
    : IRequestHandler<GetCurrentSubscriptionQuery, Result<SubscriptionResponse>>
{
    public async Task<Result<SubscriptionResponse>> Handle(GetCurrentSubscriptionQuery query, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        var hasPendingStripeEvents = subscription.StripeCustomerId is not null
                                     && await stripeEventRepository.HasPendingByStripeCustomerIdAsync(subscription.StripeCustomerId, cancellationToken);

        return new SubscriptionResponse(
            subscription.Id,
            subscription.Plan,
            subscription.ScheduledPlan,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.StripeCustomerId is not null,
            subscription.StripeSubscriptionId is not null,
            subscription.PaymentMethod,
            subscription.BillingInfo,
            subscription.FirstPaymentFailedAt is not null,
            hasPendingStripeEvents
        );
    }
}
