using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using PlatformPlatform.AccountManagement.Application.Users.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Application.Tenants.EventHandlers;

[UsedImplicitly]
public sealed class TenantCreatedEventHandler : INotificationHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;
    private readonly ISender _mediator;
    private readonly ITenantRepository _tenantRepository;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger, ITenantRepository tenantRepository,
        ISender mediator)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
        _mediator = mediator;
    }

    public async Task Handle(TenantCreatedEvent tenantCreatedEvent, CancellationToken cancellationToken)
    {
        var tenant = (await _tenantRepository.GetByIdAsync(tenantCreatedEvent.TenantId, cancellationToken))!;
        var createUserOwnerCommand = new CreateUser.Command(tenant.Email, UserRole.TenantOwner);
        var result = await _mediator.Send(createUserOwnerCommand, cancellationToken);

        if (!result.IsSuccess)
        {
            throw new Exception($"Failed to create a user for tenant {tenant.Id}. Reason: {result.ErrorMessage}");
        }

        _logger.LogInformation(@"Raise event to send Welcome mail to {TenantName}", tenant.Name);
    }
}