using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record UpdateTenantCommand : ICommand, ITenantValidation, IRequest<Result>
{
    [JsonIgnore] // Removes the Id from the API contract
    public TenantId Id { get; init; } = null!;

    public required string Name { get; init; }

    public string? Phone { get; init; }
}

[UsedImplicitly]
public sealed class UpdateTenantHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateTenantCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        tenant.Update(command.Name, command.Phone);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantUpdated(tenant.Id));

        return Result.Success();
    }
}

[UsedImplicitly]
public sealed class UpdateTenantValidator : TenantValidator<UpdateTenantCommand>;