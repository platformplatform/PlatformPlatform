using SharedKernel.Domain;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record ActivateFeatureFlagCommand(FeatureFlagKey FeatureFlagKey) : ICommand, IRequest<Result>;

public sealed class ActivateFeatureFlagValidator : AbstractValidator<ActivateFeatureFlagCommand>
{
    public ActivateFeatureFlagValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.");
    }
}

public sealed class ActivateFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<ActivateFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(ActivateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var featureFlag = await featureFlagRepository.GetBaseFeatureFlagByKeyAsync(command.FeatureFlagKey, cancellationToken);
        if (featureFlag is null) return Result.NotFound($"Feature flag with key '{command.FeatureFlagKey}' not found.");

        featureFlag.Activate(timeProvider.GetUtcNow());
        featureFlagRepository.Update(featureFlag);

        await tenantRepository.IncrementAllFeatureFlagVersionsAsync(cancellationToken);

        events.CollectEvent(new FeatureFlagActivated(command.FeatureFlagKey));

        return Result.Success();
    }
}
