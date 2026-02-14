using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

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
    DateTimeOffset? DisputedAt,
    DateTimeOffset? RefundedAt,
    bool HasPendingStripeEvents
);

public sealed class GetCurrentSubscriptionHandler(ISubscriptionRepository subscriptionRepository, IStripeEventRepository stripeEventRepository, IExecutionContext executionContext)
    : IRequestHandler<GetCurrentSubscriptionQuery, Result<SubscriptionResponse>>
{
    public async Task<Result<SubscriptionResponse>> Handle(GetCurrentSubscriptionQuery query, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

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
            subscription.DisputedAt,
            subscription.RefundedAt,
            hasPendingStripeEvents
        );
    }
}
