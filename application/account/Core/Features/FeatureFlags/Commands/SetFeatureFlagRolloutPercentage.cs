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
            .InclusiveBetween(0, RolloutBucketHasher.BucketCount).WithMessage($"Rollout percentage must be between 0 and {RolloutBucketHasher.BucketCount}.");
    }
}

public sealed class SetFeatureFlagRolloutPercentageHandler(IFeatureFlagRepository featureFlagRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetFeatureFlagRolloutPercentageCommand, Result>
{
    public async Task<Result> Handle(SetFeatureFlagRolloutPercentageCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        var fromPercentage = RolloutBucketHasher.ComputeRolloutPercentage(featureFlag.BucketStart, featureFlag.BucketEnd) ?? 0;

        int? rolloutBucketStart;
        int? rolloutBucketEnd;

        if (command.RolloutPercentage == 0)
        {
            rolloutBucketStart = null;
            rolloutBucketEnd = null;
        }
        else if (command.RolloutPercentage == RolloutBucketHasher.BucketCount)
        {
            rolloutBucketStart = 0;
            rolloutBucketEnd = RolloutBucketHasher.MaxBucketInclusive;
        }
        else
        {
            rolloutBucketStart = RolloutBucketHasher.ComputeStartingRolloutBucket(command.FlagKey);
            rolloutBucketEnd = (rolloutBucketStart.Value + command.RolloutPercentage - 1) % RolloutBucketHasher.BucketCount;
        }

        featureFlag.SetRolloutRange(rolloutBucketStart, rolloutBucketEnd);
        featureFlagRepository.Update(featureFlag);

        events.CollectEvent(new FeatureFlagRolloutPercentageUpdated(command.FlagKey, fromPercentage, command.RolloutPercentage));

        return Result.Success();
    }
}
