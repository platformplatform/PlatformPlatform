using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Shared;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ProcessPendingEventsCommand : ICommand, IRequest<Result>;

public sealed class ProcessPendingEventsHandler(ISubscriptionRepository subscriptionRepository, ProcessPendingStripeEvents processPendingStripeEvents)
    : IRequestHandler<ProcessPendingEventsCommand, Result>
{
    public async Task<Result> Handle(ProcessPendingEventsCommand command, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            return Result.BadRequest("No Stripe customer found for this subscription.");
        }

        await processPendingStripeEvents.ExecuteAsync(subscription.StripeCustomerId, cancellationToken);

        return Result.Success();
    }
}
