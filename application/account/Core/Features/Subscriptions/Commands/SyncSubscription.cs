using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Shared;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record SyncSubscriptionCommand : ICommand, IRequest<Result>;

public sealed class SyncSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    ITenantRepository tenantRepository,
    SyncSubscriptionFromStripe syncSubscriptionFromStripe,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<SyncSubscriptionHandler> logger
) : IRequestHandler<SyncSubscriptionCommand, Result>
{
    public async Task<Result> Handle(SyncSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can sync subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No Stripe customer linked to this subscription.");
        }

        await syncSubscriptionFromStripe.ExecuteAsync(subscription, cancellationToken);

        subscription.ClearPaymentFailure();
        subscription.ClearDispute();
        subscription.ClearRefund();
        subscriptionRepository.Update(subscription);

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant.State is TenantState.PastDue or TenantState.Suspended)
        {
            tenant.SetState(TenantState.Active);
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new SubscriptionSynced(subscription.Id, subscription.Plan));

        return Result.Success();
    }
}
