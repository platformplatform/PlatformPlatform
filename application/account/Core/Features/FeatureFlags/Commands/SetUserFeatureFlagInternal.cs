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
public sealed record SetUserFeatureFlagInternalCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public required UserId UserId { get; init; }

    public required TenantId TenantId { get; init; }

    public required bool Enabled { get; init; }
}

public sealed class SetUserFeatureFlagInternalValidator : AbstractValidator<SetUserFeatureFlagInternalCommand>
{
    public SetUserFeatureFlagInternalValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");
    }
}

public sealed class SetUserFeatureFlagInternalHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetUserFeatureFlagInternalCommand, Result>
{
    public async Task<Result> Handle(SetUserFeatureFlagInternalCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(command.UserId, cancellationToken);
        if (user is null || user.TenantId != command.TenantId)
        {
            return Result.NotFound($"User '{command.UserId}' not found in tenant '{command.TenantId}'.");
        }

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            var userOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, command.UserId, cancellationToken);
            if (userOverride is null)
            {
                userOverride = FeatureFlag.CreateUserOverride(command.FlagKey, command.TenantId, command.UserId);
                userOverride.Activate(now);
                await featureFlagRepository.AddAsync(userOverride, cancellationToken);
            }
            else
            {
                userOverride.Activate(now);
                featureFlagRepository.Update(userOverride);
            }

            events.CollectEvent(new FeatureFlagUserOverrideSet(command.FlagKey, command.UserId.Value));
        }
        else
        {
            var userOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, command.UserId, cancellationToken);
            if (userOverride is not null)
            {
                userOverride.Deactivate(now);
                featureFlagRepository.Update(userOverride);
                events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FlagKey, command.UserId.Value));
            }
        }

        return Result.Success();
    }
}
