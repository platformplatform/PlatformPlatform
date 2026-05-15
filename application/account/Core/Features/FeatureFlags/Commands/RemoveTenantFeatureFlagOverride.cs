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
public sealed record RemoveTenantFeatureFlagOverrideCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public required TenantId TenantId { get; init; }
}

public sealed class RemoveTenantFeatureFlagOverrideValidator : AbstractValidator<RemoveTenantFeatureFlagOverrideCommand>
{
    public RemoveTenantFeatureFlagOverrideValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.RequiredPlan is null).WithMessage("Plan-gated feature flags cannot be removed manually; change the tenant's subscription plan instead.");
    }
}

public sealed class RemoveTenantFeatureFlagOverrideHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveTenantFeatureFlagOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveTenantFeatureFlagOverrideCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.NotFound($"Tenant '{command.TenantId}' not found.");
        }

        var tenantOverride = await featureFlagRepository.GetByKeyAndScopeAsync(command.FlagKey, command.TenantId, null, cancellationToken);
        if (tenantOverride is null) return Result.NotFound($"No tenant override found for flag '{command.FlagKey}' and tenant '{command.TenantId}'.");

        featureFlagRepository.Remove(tenantOverride);

        events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FlagKey, command.TenantId, FeatureFlagOverrideTrigger.Internal));

        return Result.Success();
    }
}
