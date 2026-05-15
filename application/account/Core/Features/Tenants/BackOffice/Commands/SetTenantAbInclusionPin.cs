using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.BackOffice.Commands;

[PublicAPI]
public sealed record SetTenantAbInclusionPinCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;

    public AbInclusionPin? AbInclusionPin { get; init; }
}

public sealed class SetTenantAbInclusionPinHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantAbInclusionPinCommand, Result>
{
    public async Task<Result> Handle(SetTenantAbInclusionPinCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.TenantId}' not found.");

        var fromPin = tenant.AbInclusionPin;
        tenant.SetAbInclusionPin(command.AbInclusionPin);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantAbInclusionPinUpdated(tenant.Id, fromPin, command.AbInclusionPin));

        return Result.Success();
    }
}
