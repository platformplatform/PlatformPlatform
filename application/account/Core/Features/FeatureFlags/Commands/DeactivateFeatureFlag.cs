using Account.Features.FeatureFlags.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record DeactivateFeatureFlagCommand(string FlagKey) : ICommand, IRequest<Result>;

public sealed class DeactivateFeatureFlagValidator : AbstractValidator<DeactivateFeatureFlagCommand>
{
    public DeactivateFeatureFlagValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.IsKillSwitchEnabled == true).WithMessage("Only kill-switch-enabled feature flags can be globally deactivated.");
    }
}

public sealed class DeactivateFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<DeactivateFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(DeactivateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        featureFlag.Deactivate(timeProvider.GetUtcNow());
        featureFlagRepository.Update(featureFlag);

        events.CollectEvent(new FeatureFlagDeactivated(command.FlagKey));

        return Result.Success();
    }
}
