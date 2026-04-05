using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetTenantFeatureFlagOwnerCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FlagKey { get; init; } = null!;

    public required bool Enabled { get; init; }
}

public sealed class SetTenantFeatureFlagOwnerValidator : AbstractValidator<SetTenantFeatureFlagOwnerCommand>
{
    public SetTenantFeatureFlagOwnerValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class SetTenantFeatureFlagOwnerHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagOwnerCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagOwnerCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to configure tenant feature flags.");
        }

        var featureFlagDefinition = SharedKernel.FeatureFlags.FeatureFlags.Get(command.FlagKey);
        if (featureFlagDefinition is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        if (featureFlagDefinition.AdminLevel != FeatureFlagAdminLevel.TenantOwner || !featureFlagDefinition.ConfigurableByTenant)
        {
            return Result.Forbidden($"Feature flag '{command.FlagKey}' is not configurable by tenant owners.");
        }

        var tenantId = executionContext.TenantId!.Value;
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, null, cancellationToken);
            if (tenantFeatureFlag is null)
            {
                tenantFeatureFlag = FeatureFlag.CreateTenantOverride(command.FlagKey, tenantId);
                tenantFeatureFlag.Activate(now);
                await featureFlagRepository.AddAsync(tenantFeatureFlag, cancellationToken);
            }
            else
            {
                tenantFeatureFlag.Activate(now);
                featureFlagRepository.Update(tenantFeatureFlag);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FlagKey, tenantId.ToString()));
        }
        else
        {
            var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, null, cancellationToken);
            if (tenantFeatureFlag is not null)
            {
                tenantFeatureFlag.Deactivate(now);
                featureFlagRepository.Update(tenantFeatureFlag);
                events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, tenantId.ToString()));
            }
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        tenant!.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
