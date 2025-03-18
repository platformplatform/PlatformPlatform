using PlatformPlatform.AccountManagement.Features.Tenants.Domain;

namespace PlatformPlatform.AccountManagement.Features.Tenants.EventHandlers;

public sealed class TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger)
    : INotificationHandler<TenantCreatedEvent>
{
    public Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Raise event to send Welcome mail to tenant");

        return Task.CompletedTask;
    }
}
