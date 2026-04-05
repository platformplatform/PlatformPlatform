using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetUserFeatureFlagCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FlagKey { get; init; } = null!;

    public required bool Enabled { get; init; }
}

public sealed class SetUserFeatureFlagValidator : AbstractValidator<SetUserFeatureFlagCommand>
{
    public SetUserFeatureFlagValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");
    }
}

public sealed class SetUserFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetUserFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(SetUserFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var featureFlagDefinition = SharedKernel.FeatureFlags.FeatureFlags.Get(command.FlagKey);
        if (featureFlagDefinition is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        if (featureFlagDefinition.AdminLevel != FeatureFlagAdminLevel.User || !featureFlagDefinition.ConfigurableByUser)
        {
            return Result.Forbidden($"Feature flag '{command.FlagKey}' is not configurable by users.");
        }

        var tenantId = executionContext.TenantId!.Value;
        var userId = executionContext.UserInfo.Id!.ToString();
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var userFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, userId, cancellationToken);
            if (userFeatureFlag is null)
            {
                userFeatureFlag = FeatureFlag.CreateUserOverride(command.FlagKey, tenantId, userId);
                userFeatureFlag.Activate(now);
                await featureFlagRepository.AddAsync(userFeatureFlag, cancellationToken);
            }
            else
            {
                userFeatureFlag.Activate(now);
                featureFlagRepository.Update(userFeatureFlag);
            }

            events.CollectEvent(new FeatureFlagUserOverrideSet(command.FlagKey, userId));
        }
        else
        {
            var userFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, userId, cancellationToken);
            if (userFeatureFlag is not null)
            {
                userFeatureFlag.Deactivate(now);
                featureFlagRepository.Update(userFeatureFlag);
                events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FlagKey, userId));
            }
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        tenant!.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
