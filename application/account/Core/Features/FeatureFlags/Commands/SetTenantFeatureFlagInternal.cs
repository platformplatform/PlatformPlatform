using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetTenantFeatureFlagInternalCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FlagKey { get; init; } = null!;

    public required long TenantId { get; init; }

    public required bool Enabled { get; init; }
}

public sealed class SetTenantFeatureFlagInternalValidator : AbstractValidator<SetTenantFeatureFlagInternalCommand>
{
    public SetTenantFeatureFlagInternalValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class SetTenantFeatureFlagInternalHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagInternalCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagInternalCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with ID '{command.TenantId}' not found.");

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantFeatureFlag is null)
            {
                tenantFeatureFlag = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId);
                tenantFeatureFlag.Activate(now);
                await featureFlagRepository.AddAsync(tenantFeatureFlag, cancellationToken);
            }
            else
            {
                tenantFeatureFlag.Activate(now);
                featureFlagRepository.Update(tenantFeatureFlag);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FlagKey, command.TenantId.ToString()));
        }
        else
        {
            var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantFeatureFlag is null)
            {
                tenantFeatureFlag = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId);
                await featureFlagRepository.AddAsync(tenantFeatureFlag, cancellationToken);
            }
            else
            {
                tenantFeatureFlag.Deactivate(now);
                featureFlagRepository.Update(tenantFeatureFlag);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, command.TenantId.ToString()));
        }

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
