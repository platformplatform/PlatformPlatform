using Account.Features.FeatureFlags.Shared;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetTenantFeatureFlagInternalCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FeatureFlagKey { get; init; } = null!;

    public required TenantId TenantId { get; init; }

    public required bool Enabled { get; init; }
}

public sealed class SetTenantFeatureFlagInternalValidator : AbstractValidator<SetTenantFeatureFlagInternalCommand>
{
    public SetTenantFeatureFlagInternalValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class SetTenantFeatureFlagInternalHandler(TenantFeatureFlagToggler tenantFeatureFlagToggler, ITenantRepository tenantRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagInternalCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagInternalCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with ID '{command.TenantId}' not found.");

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            await tenantFeatureFlagToggler.EnableAsync(command.FeatureFlagKey, command.TenantId, now, cancellationToken);
            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FeatureFlagKey, command.TenantId.ToString()));
        }
        else
        {
            await tenantFeatureFlagToggler.DisableAsync(command.FeatureFlagKey, command.TenantId, now, cancellationToken);
            events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FeatureFlagKey, command.TenantId.ToString()));
        }

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
