using Account.Features.Subscriptions.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.BackOffice.Commands;

[PublicAPI]
public sealed record AcknowledgeBillingDriftCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;
}

public sealed class AcknowledgeBillingDriftHandler(
    ISubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<AcknowledgeBillingDriftCommand, Result>
{
    public async Task<Result> Handle(AcknowledgeBillingDriftCommand command, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (subscription is null) return Result.NotFound($"Subscription for tenant '{command.TenantId}' not found.");

        if (!subscription.HasDriftDetected) return Result.BadRequest("Subscription has no drift to acknowledge.");

        subscription.AcknowledgeDrift(timeProvider.GetUtcNow());
        subscriptionRepository.Update(subscription);

        events.CollectEvent(new TenantBillingDriftAcknowledged(subscription.Id));

        return Result.Success();
    }
}
