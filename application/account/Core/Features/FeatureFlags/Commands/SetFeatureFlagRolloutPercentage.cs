using Account.Features.FeatureFlags.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetFeatureFlagRolloutPercentageCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FlagKey { get; init; } = null!;

    public required int RolloutPercentage { get; init; }
}

public sealed class SetFeatureFlagRolloutPercentageValidator : AbstractValidator<SetFeatureFlagRolloutPercentageCommand>
{
    public SetFeatureFlagRolloutPercentageValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.IsAbTestEligible == true).WithMessage("Feature flag must be eligible for A/B testing.");

        RuleFor(x => x.RolloutPercentage)
            .InclusiveBetween(0, 100).WithMessage("Rollout percentage must be between 0 and 100.");
    }
}

public sealed class SetFeatureFlagRolloutPercentageHandler(IFeatureFlagRepository featureFlagRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetFeatureFlagRolloutPercentageCommand, Result>
{
    public async Task<Result> Handle(SetFeatureFlagRolloutPercentageCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature featureFlag with key '{command.FlagKey}' not found.");

        int? rolloutBucketStart;
        int? rolloutBucketEnd;

        if (command.RolloutPercentage == 0)
        {
            rolloutBucketStart = null;
            rolloutBucketEnd = null;
        }
        else if (command.RolloutPercentage == 100)
        {
            rolloutBucketStart = 0;
            rolloutBucketEnd = 99;
        }
        else
        {
            rolloutBucketStart = RolloutBucketHasher.ComputeStartingRolloutBucket(command.FlagKey);
            rolloutBucketEnd = (rolloutBucketStart.Value + command.RolloutPercentage - 1) % 100;
        }

        featureFlag.SetRolloutRange(rolloutBucketStart, rolloutBucketEnd);
        featureFlagRepository.Update(featureFlag);

        events.CollectEvent(new FeatureFlagRolloutPercentageUpdated(command.FlagKey, command.RolloutPercentage));

        return Result.Success();
    }
}
