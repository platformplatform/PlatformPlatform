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
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantOverride is null)
            {
                tenantOverride = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId);
                tenantOverride.Activate(now);
                await featureFlagRepository.AddAsync(tenantOverride, cancellationToken);
            }
            else
            {
                tenantOverride.Activate(now);
                featureFlagRepository.Update(tenantOverride);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FlagKey, command.TenantId.ToString()));
        }
        else
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
            if (tenantOverride is null)
            {
                tenantOverride = FeatureFlag.CreateTenantOverride(command.FlagKey, command.TenantId);
                await featureFlagRepository.AddAsync(tenantOverride, cancellationToken);
            }
            else
            {
                tenantOverride.Deactivate(now);
                featureFlagRepository.Update(tenantOverride);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, command.TenantId.ToString()));
        }

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is not null)
        {
            tenant.IncrementFeatureFlagVersion();
            tenantRepository.Update(tenant);
        }

        return Result.Success();
    }
}
