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
    bool HasStripeCustomer,
    bool HasStripeSubscription,
    decimal? CurrentPriceAmount,
    string? CurrentPriceCurrency,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    bool IsPaymentFailed,
    PaymentMethod? PaymentMethod,
    BillingInfo? BillingInfo,
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
            subscription.StripeCustomerId is not null,
            subscription.StripeSubscriptionId is not null,
            subscription.CurrentPriceAmount,
            subscription.CurrentPriceCurrency,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.FirstPaymentFailedAt is not null,
            subscription.PaymentMethod,
            subscription.BillingInfo,
            hasPendingStripeEvents
        );
    }
}
