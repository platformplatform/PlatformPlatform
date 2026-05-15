using Account.Features.FeatureFlags.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record ActivateFeatureFlagCommand(string FlagKey) : ICommand, IRequest<Result>;

public sealed class ActivateFeatureFlagValidator : AbstractValidator<ActivateFeatureFlagCommand>
{
    public ActivateFeatureFlagValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.IsKillSwitchEnabled == true).WithMessage("Only kill-switch-enabled feature flags can be globally activated.");
    }
}

public sealed class ActivateFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<ActivateFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(ActivateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        featureFlag.Activate(timeProvider.GetUtcNow());
        featureFlagRepository.Update(featureFlag);

        events.CollectEvent(new FeatureFlagActivated(command.FlagKey));

        return Result.Success();
    }
}
