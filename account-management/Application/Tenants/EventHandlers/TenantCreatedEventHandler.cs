using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.EventHandlers;

[UsedImplicitly]
public sealed class TenantCreatedEventHandler : INotificationHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;
    private readonly ITenantRepository _tenantRepository;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger, ITenantRepository tenantRepository)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
    }

    public async Task Handle(TenantCreatedEvent notification, CancellationToken cancellationToken)
    {
        var tenant = (await _tenantRepository.GetByIdAsync(notification.TenantId, cancellationToken))!;

        _logger.LogInformation(@"Raise event to send Welcome mail to {TenantName}", tenant.Name);
    }
}