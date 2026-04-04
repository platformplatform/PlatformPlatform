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

public sealed class SetFeatureFlagRolloutPercentageHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
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
            bucketStart = 0;
            bucketEnd = 99;
        }
        else
        {
            bucketStart = ComputeStartingBucket(command.FlagKey);
            bucketEnd = (bucketStart.Value + command.RolloutPercentage - 1) % 100;
        }

        flag.SetRolloutRange(bucketStart, bucketEnd);
        featureFlagRepository.Update(flag);

        await tenantRepository.IncrementAllFeatureFlagVersionsAsync(cancellationToken);

        events.CollectEvent(new FeatureFlagRolloutPercentageUpdated(command.FlagKey, command.RolloutPercentage));

        return Result.Success();
    }

    private static int ComputeStartingBucket(string flagKey)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in flagKey)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return (int)(hash % 100);
        }
    }
}
