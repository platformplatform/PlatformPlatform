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
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.");
    }
}

public sealed class ActivateFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<ActivateFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(ActivateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var flag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (flag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        flag.Activate(timeProvider.GetUtcNow());
        featureFlagRepository.Update(flag);

        events.CollectEvent(new FeatureFlagActivated(command.FlagKey));

        return Result.Success();
    }
}
