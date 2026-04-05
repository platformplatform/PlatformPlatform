using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
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

    public required string UserId { get; init; }

    public required long TenantId { get; init; }
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

public sealed class RemoveUserFeatureFlagOverrideHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveUserFeatureFlagOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveUserFeatureFlagOverrideCommand command, CancellationToken cancellationToken)
    {
        var userFeatureFlag = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, command.UserId, cancellationToken);
        if (userFeatureFlag is null) return Result.NotFound($"No user override found for flag '{command.FlagKey}' and user '{command.UserId}'.");

        featureFlagRepository.Remove(userFeatureFlag);

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is not null)
        {
            tenant.IncrementFeatureFlagVersion();
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FlagKey, command.UserId));

        return Result.Success();
    }
}
