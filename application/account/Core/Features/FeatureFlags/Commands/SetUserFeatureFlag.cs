using Account.Features.FeatureFlags.Domain;
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
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Flag must have user scope.");
    }
}

public sealed class SetUserFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetUserFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(SetUserFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(command.FlagKey);
        if (definition is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        if (definition.AdminLevel != FeatureFlagAdminLevel.User || !definition.ConfigurableByUser)
        {
            return Result.Forbidden($"Feature flag '{command.FlagKey}' is not configurable by users.");
        }

        var tenantId = executionContext.TenantId!.Value;
        var userId = executionContext.UserInfo.Id!.ToString();
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var userOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, userId, cancellationToken);
            if (userOverride is null)
            {
                userOverride = FeatureFlag.CreateUserOverride(command.FlagKey, tenantId, userId);
                userOverride.Activate(now);
                await featureFlagRepository.AddAsync(userOverride, cancellationToken);
            }
            else
            {
                userOverride.Activate(now);
                featureFlagRepository.Update(userOverride);
            }

            events.CollectEvent(new FeatureFlagUserOverrideSet(command.FlagKey, userId));
        }
        else
        {
            var userOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, tenantId, userId, cancellationToken);
            if (userOverride is not null)
            {
                userOverride.Deactivate(now);
                featureFlagRepository.Update(userOverride);
                events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FlagKey, userId));
            }
        }

        return Result.Success();
    }
}
