using SharedKernel.Domain;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetFeatureFlagRolloutPercentageCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public FeatureFlagKey FeatureFlagKey { get; init; } = null!;

    public required int RolloutPercentage { get; init; }
}

public sealed class SetFeatureFlagRolloutPercentageValidator : AbstractValidator<SetFeatureFlagRolloutPercentageCommand>
{
    public SetFeatureFlagRolloutPercentageValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.IsAbTestEligible == true).WithMessage("Feature flag must be eligible for A/B testing.");

        RuleFor(x => x.RolloutPercentage)
            .InclusiveBetween(0, 100).WithMessage("Rollout percentage must be between 0 and 100.");
    }
}

public sealed class SetFeatureFlagRolloutPercentageHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetFeatureFlagRolloutPercentageCommand, Result>
{
    public async Task<Result> Handle(SetFeatureFlagRolloutPercentageCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetBaseFeatureFlagByKeyAsync(command.FeatureFlagKey, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature flag with key '{command.FeatureFlagKey}' not found.");

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
            rolloutBucketStart = ComputeStartingRolloutBucket(command.FeatureFlagKey);
            rolloutBucketEnd = (rolloutBucketStart.Value + command.RolloutPercentage - 1) % 100;
        }

        featureFlag.SetRolloutRange(rolloutBucketStart, rolloutBucketEnd);
        featureFlagRepository.Update(featureFlag);

        await tenantRepository.IncrementAllFeatureFlagVersionsAsync(cancellationToken);

        events.CollectEvent(new FeatureFlagRolloutPercentageUpdated(command.FeatureFlagKey, command.RolloutPercentage));

        return Result.Success();
    }

    private static int ComputeStartingRolloutBucket(FeatureFlagKey featureFlagKey)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in featureFlagKey.Value)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return (int)(hash % 100);
        }
    }
}
