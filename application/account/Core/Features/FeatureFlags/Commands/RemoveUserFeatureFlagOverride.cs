using Account.Features.FeatureFlags.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record RemoveUserFeatureFlagOverrideCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public required UserId UserId { get; init; }

    public required TenantId TenantId { get; init; }
}

public sealed class RemoveUserFeatureFlagOverrideValidator : AbstractValidator<RemoveUserFeatureFlagOverrideCommand>
{
    public RemoveUserFeatureFlagOverrideValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");
    }
}

public sealed class RemoveUserFeatureFlagOverrideHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveUserFeatureFlagOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveUserFeatureFlagOverrideCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(command.UserId, cancellationToken);
        if (user is null || user.TenantId != command.TenantId)
        {
            return Result.NotFound($"User '{command.UserId}' not found in tenant '{command.TenantId}'.");
        }

        var userOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, command.UserId, cancellationToken);
        if (userOverride is null) return Result.NotFound($"No user override found for flag '{command.FlagKey}' and user '{command.UserId}'.");

        featureFlagRepository.Remove(userOverride);

        events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FlagKey, command.UserId, FeatureFlagOverrideTrigger.Internal));

        return Result.Success();
    }
}
