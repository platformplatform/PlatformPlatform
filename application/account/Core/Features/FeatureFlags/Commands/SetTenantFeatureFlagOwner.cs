using Account.Features.FeatureFlags.Domain;
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
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Flag must have tenant scope.");
    }
}

public sealed class SetTenantFeatureFlagOwnerHandler(IFeatureFlagRepository featureFlagRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagOwnerCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagOwnerCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to configure tenant feature flags.");
        }

        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(command.FlagKey);
        if (definition is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        if (definition.AdminLevel != FeatureFlagAdminLevel.TenantOwner || !definition.ConfigurableByTenant)
        {
            return Result.Forbidden($"Feature flag '{command.FlagKey}' is not configurable by tenant owners.");
        }

        var tenantId = executionContext.TenantId!.Value;
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, null, cancellationToken);
            if (tenantOverride is null)
            {
                tenantOverride = FeatureFlag.CreateTenantOverride(command.FlagKey, tenantId);
                tenantOverride.Activate(now);
                await featureFlagRepository.AddAsync(tenantOverride, cancellationToken);
            }
            else
            {
                tenantOverride.Activate(now);
                featureFlagRepository.Update(tenantOverride);
            }

            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FlagKey, tenantId.ToString()));
        }
        else
        {
            var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, null, cancellationToken);
            if (tenantOverride is not null)
            {
                tenantOverride.Deactivate(now);
                featureFlagRepository.Update(tenantOverride);
                events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, tenantId.ToString()));
            }
        }

        return Result.Success();
    }
}
