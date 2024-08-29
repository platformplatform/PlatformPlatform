using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.AccountManagement.Core.Users.Commands;
using PlatformPlatform.AccountManagement.Core.Users.Domain;

namespace PlatformPlatform.AccountManagement.Core.Tenants.EventHandlers;

public sealed class TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger, ISender mediator)
    : INotificationHandler<TenantCreatedEvent>
{
    public async Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        var createTenantOwnerCommand
            = new CreateUserCommand(notification.TenantId, notification.Email, UserRole.Owner, true);
        var result = await mediator.Send(createTenantOwnerCommand, cancellationToken);

        if (!result.IsSuccess) throw new UnreachableException($"Create Tenant Owner: {result.GetErrorSummary()}");

        logger.LogInformation("Raise event to send Welcome mail to tenant.");
    }
}
