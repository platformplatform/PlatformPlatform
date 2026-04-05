using Account.Features.FeatureFlags.Shared;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetTenantFeatureFlagOwnerCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public FeatureFlagKey FeatureFlagKey { get; init; } = null!;

    public required bool Enabled { get; init; }
}

public sealed class SetTenantFeatureFlagOwnerValidator : AbstractValidator<SetTenantFeatureFlagOwnerCommand>
{
    public SetTenantFeatureFlagOwnerValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class SetTenantFeatureFlagOwnerHandler(TenantFeatureFlagToggler tenantFeatureFlagToggler, ITenantRepository tenantRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetTenantFeatureFlagOwnerCommand, Result>
{
    public async Task<Result> Handle(SetTenantFeatureFlagOwnerCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to configure tenant feature flags.");
        }

        var featureFlagDefinition = SharedKernel.Domain.FeatureFlags.Get(command.FeatureFlagKey);
        if (featureFlagDefinition is null) return Result.NotFound($"Feature flag with key '{command.FeatureFlagKey}' not found.");

        if (featureFlagDefinition.AdminLevel != FeatureFlagAdminLevel.TenantOwner || !featureFlagDefinition.ConfigurableByTenant)
        {
            return Result.Forbidden($"Feature flag '{command.FeatureFlagKey}' is not configurable by tenant owners.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null) return Result.NotFound("Tenant not found.");

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            await tenantFeatureFlagToggler.EnableAsync(command.FeatureFlagKey, tenant.Id, now, cancellationToken);
            events.CollectEvent(new FeatureFlagTenantOverrideSet(command.FeatureFlagKey, tenant.Id.ToString()));
        }
        else
        {
            await tenantFeatureFlagToggler.DisableAsync(command.FeatureFlagKey, tenant.Id, now, cancellationToken);
            events.CollectEvent(new FeatureFlagTenantOverrideRemoved(command.FeatureFlagKey, tenant.Id.ToString()));
        }

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
