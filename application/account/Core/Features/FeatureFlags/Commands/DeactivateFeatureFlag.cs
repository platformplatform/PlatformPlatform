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
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.");
    }
}

public sealed class DeactivateFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<DeactivateFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(DeactivateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var flag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (flag is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        flag.Deactivate(timeProvider.GetUtcNow());
        featureFlagRepository.Update(flag);

        events.CollectEvent(new FeatureFlagDeactivated(command.FlagKey));

        return Result.Success();
    }
}
