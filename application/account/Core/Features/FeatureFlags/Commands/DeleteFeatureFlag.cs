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

public sealed class DeleteFeatureFlagHandler(
    IFeatureFlagRepository featureFlagRepository,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<DeleteFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(DeleteFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, null, null, cancellationToken);
        if (baseRow is null) return Result.NotFound($"Feature flag with key '{command.FlagKey}' not found.");

        // Only orphaned flags can be deleted. Live flags must first be removed from FeatureFlags.cs so the
        // reconciler marks them orphaned on the next deploy; this preserves the audit trail and lets admins
        // review impact before the row is retired.
        if (baseRow.OrphanedAt is null) return Result.BadRequest($"Feature flag '{command.FlagKey}' is not orphaned - only flags removed from the C# definitions can be deleted.");

        if (baseRow.DeletedAt is not null) return Result.BadRequest($"Feature flag '{command.FlagKey}' is already deleted.");

        // Soft-delete the base row to retain the flag key for historical telemetry; hard-delete the
        // tenant and user override rows because they carry no historical value once the flag is retired.
        var now = timeProvider.GetUtcNow();
        var daysSinceOrphaned = (int)(now - baseRow.OrphanedAt.Value).TotalDays;
        baseRow.MarkDeleted(now);
        featureFlagRepository.Update(baseRow);

        var overrides = await featureFlagRepository.GetRowsByFlagKeyUnfilteredAsync(command.FlagKey, cancellationToken);
        var overrideRowsToRemove = overrides.Where(r => r.TenantId is not null || r.UserId is not null).ToArray();
        foreach (var row in overrideRowsToRemove)
        {
            featureFlagRepository.Remove(row);
        }

        events.CollectEvent(new FeatureFlagDeleted(command.FlagKey, overrideRowsToRemove.Length, daysSinceOrphaned));

        return Result.Success();
    }
}
