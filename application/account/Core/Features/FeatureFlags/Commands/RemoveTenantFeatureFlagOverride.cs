using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record RemoveTenantFeatureFlagOverrideCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public string FeatureFlagKey { get; init; } = null!;

    public required TenantId TenantId { get; init; }
}

public sealed class RemoveTenantFeatureFlagOverrideValidator : AbstractValidator<RemoveTenantFeatureFlagOverrideCommand>
{
    public RemoveTenantFeatureFlagOverrideValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class RemoveTenantFeatureFlagOverrideHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveTenantFeatureFlagOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveTenantFeatureFlagOverrideCommand command, CancellationToken cancellationToken)
    {
        var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndTenantAsync(command.FeatureFlagKey, command.TenantId, cancellationToken);
        if (tenantFeatureFlag is null) return Result.NotFound($"No tenant override found for flag '{command.FeatureFlagKey}' and tenant '{command.TenantId}'.");

        featureFlagRepository.Remove(tenantFeatureFlag);

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with ID '{command.TenantId}' not found.");

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FeatureFlagKey, command.TenantId.ToString()));

        return Result.Success();
    }
}
