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
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.IsAbTestEligible == true).WithMessage("Flag must be eligible for A/B testing.");

        RuleFor(x => x.RolloutPercentage)
            .InclusiveBetween(0, 100).WithMessage("Rollout percentage must be between 0 and 100.");
    }
}

public sealed class SetFeatureFlagRolloutPercentageHandler(IFeatureFlagRepository featureFlagRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetFeatureFlagRolloutPercentageCommand, Result>
{
    public async Task<Result> Handle(SetFeatureFlagRolloutPercentageCommand command, CancellationToken cancellationToken)
    {
        var flag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (flag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        int? bucketStart;
        int? bucketEnd;

        if (command.RolloutPercentage == 0)
        {
            bucketStart = null;
            bucketEnd = null;
        }
        else if (command.RolloutPercentage == 100)
        {
            bucketStart = 1;
            bucketEnd = 100;
        }
        else
        {
            bucketStart = RolloutBucketHasher.ComputeBucket(command.FlagKey);
            bucketEnd = (bucketStart - 1 + command.RolloutPercentage) % 100 + 1;
        }

        flag.SetRolloutRange(bucketStart, bucketEnd);
        featureFlagRepository.Update(flag);

        events.CollectEvent(new FeatureFlagRolloutPercentageUpdated(command.FlagKey, command.RolloutPercentage));

        return Result.Success();
    }
}
