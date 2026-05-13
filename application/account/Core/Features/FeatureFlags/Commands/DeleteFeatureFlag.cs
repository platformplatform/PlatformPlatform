using Account.Features.FeatureFlags.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record DeleteFeatureFlagCommand(string FlagKey) : ICommand, IRequest<Result>;

public sealed class DeleteFeatureFlagValidator : AbstractValidator<DeleteFeatureFlagCommand>
{
    public DeleteFeatureFlagValidator()
    {
        RuleFor(x => x.FlagKey).NotEmpty().WithMessage("Feature flag key must not be empty.");
    }
}

public sealed class DeleteFeatureFlagHandler(IFeatureFlagRepository featureFlagRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(DeleteFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (baseRow is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        // Only orphaned flags can be hard-deleted. Live flags must instead be removed from FeatureFlags.cs
        // and let the reconciler mark them orphaned on the next deploy; this preserves the audit trail and
        // ensures tenants/users keep the behavior they had at the moment the flag was removed from code.
        if (baseRow.OrphanedAt is null) return Result.BadRequest($"Feature flag '{command.FlagKey}' is not orphaned - only flags removed from the C# definitions can be deleted.");

        var rowsToRemove = await featureFlagRepository.GetRowsByFlagKeyUnfilteredAsync(command.FlagKey, cancellationToken);
        foreach (var row in rowsToRemove)
        {
            featureFlagRepository.Remove(row);
        }

        events.CollectEvent(new FeatureFlagDeleted(command.FlagKey));

        return Result.Success();
    }
}
