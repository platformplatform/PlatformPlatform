namespace PlatformPlatform.AccountManagement.Application.Tenants;

[UsedImplicitly]
public sealed class TenantCreatedEventHandler : INotificationHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Raise event to send Welcome mail to tenant");
        return Task.CompletedTask;
    }
}