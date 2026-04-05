using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record RemoveUserFeatureFlagOverrideCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public string FeatureFlagKey { get; init; } = null!;

    public required UserId UserId { get; init; }

    public required TenantId TenantId { get; init; }
}

public sealed class RemoveUserFeatureFlagOverrideValidator : AbstractValidator<RemoveUserFeatureFlagOverrideCommand>
{
    public RemoveUserFeatureFlagOverrideValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");
    }
}

public sealed class RemoveUserFeatureFlagOverrideHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveUserFeatureFlagOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveUserFeatureFlagOverrideCommand command, CancellationToken cancellationToken)
    {
        var userFeatureFlag = await featureFlagRepository.GetByKeyAndUserAsync(command.FeatureFlagKey, command.TenantId, command.UserId, cancellationToken);
        if (userFeatureFlag is null) return Result.NotFound($"No user override found for flag '{command.FeatureFlagKey}' and user '{command.UserId}'.");

        featureFlagRepository.Remove(userFeatureFlag);

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with ID '{command.TenantId}' not found.");

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FeatureFlagKey, command.UserId.ToString()));

        return Result.Success();
    }
}
