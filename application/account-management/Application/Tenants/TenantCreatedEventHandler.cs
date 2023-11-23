namespace PlatformPlatform.AccountManagement.Application.Tenants;

[UsedImplicitly]
public sealed class TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger)
    : INotificationHandler<TenantCreatedEvent>
{
    public Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Raise event to send Welcome mail to tenant.");
        return Task.CompletedTask;
    }
}