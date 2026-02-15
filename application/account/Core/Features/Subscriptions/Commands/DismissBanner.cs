using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record DismissBannerCommand(BannerType BannerType) : ICommand, IRequest<Result>;

public sealed class DismissBannerHandler(ISubscriptionRepository subscriptionRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<DismissBannerCommand, Result>
{
    public async Task<Result> Handle(DismissBannerCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        switch (command.BannerType)
        {
            case BannerType.Refund when subscription.RefundedAt is null:
                return Result.BadRequest("No active refund banner to dismiss.");
            case BannerType.Refund:
                subscription.ClearRefund();
                break;
            case BannerType.Dispute when subscription.DisputedAt is null:
                return Result.BadRequest("No active dispute banner to dismiss.");
            case BannerType.Dispute:
                subscription.ClearDispute();
                break;
            default:
                throw new UnreachableException($"Unknown banner type '{command.BannerType}'.");
        }

        subscriptionRepository.Update(subscription);

        events.CollectEvent(new BannerDismissed(subscription.Id, command.BannerType));

        return Result.Success();
    }
}
