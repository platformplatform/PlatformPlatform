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

    public required TenantId TenantId { get; init; }

    public required bool Enabled { get; init; }
}

public sealed class SetTenantFeatureFlagInternalValidator : AbstractValidator<SetTenantFeatureFlagInternalCommand>
{
    public SetTenantFeatureFlagInternalValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.RequiredPlan is null).WithMessage("Plan-gated feature flags cannot be set manually; change the tenant's subscription plan instead.");
    }
}

public sealed class SetTenantFeatureFlagInternalHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagInternalCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagInternalCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.NotFound($"Tenant '{command.TenantId}' not found.");
        }

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantOverride is null)
            {
                tenantOverride = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId, FeatureFlagScope.Tenant);
                tenantOverride.Activate(now);
                await featureFlagRepository.AddAsync(tenantOverride, cancellationToken);
            }
            else
            {
                tenantOverride.Activate(now);
                featureFlagRepository.Update(tenantOverride);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FlagKey, command.TenantId, FeatureFlagOverrideTrigger.Internal));
        }
        else
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantOverride is null)
            {
                // Create the override row in a disabled state. Without an explicit override, the evaluator
                // falls back to the base row + rollout, so a tenant currently enabled-by-rollout would stay
                // enabled if we just no-op'd here. The row's EnabledAt == DisabledAt makes IsActive=false.
                tenantOverride = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId, FeatureFlagScope.Tenant);
                tenantOverride.Activate(now);
                tenantOverride.Deactivate(now);
                await featureFlagRepository.AddAsync(tenantOverride, cancellationToken);
            }
            else
            {
                tenantOverride.Deactivate(now);
                featureFlagRepository.Update(tenantOverride);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, command.TenantId, FeatureFlagOverrideTrigger.Internal));
        }

        return Result.Success();
    }
}
