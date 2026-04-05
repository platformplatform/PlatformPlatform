using Account.Features.FeatureFlags.Shared;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetUserFeatureFlagCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public FeatureFlagKey FeatureFlagKey { get; init; } = null!;

    public required bool Enabled { get; init; }
}

public sealed class SetUserFeatureFlagValidator : AbstractValidator<SetUserFeatureFlagCommand>
{
    public SetUserFeatureFlagValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");
    }
}

public sealed class SetUserFeatureFlagHandler(UserFeatureFlagToggler userFeatureFlagToggler, IUserRepository userRepository, ITenantRepository tenantRepository, TimeProvider timeProvider, ITelemetryEventsCollector events)
    : IRequestHandler<SetUserFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(SetUserFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var featureFlagDefinition = SharedKernel.Domain.FeatureFlags.Get(command.FeatureFlagKey);
        if (featureFlagDefinition is null) return Result.NotFound($"Feature flag with key '{command.FeatureFlagKey}' not found.");

        if (featureFlagDefinition.AdminLevel != FeatureFlagAdminLevel.User || !featureFlagDefinition.ConfigurableByUser)
        {
            return Result.Forbidden($"Feature flag '{command.FeatureFlagKey}' is not configurable by users.");
        }

        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null) return Result.NotFound("Tenant not found.");

        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            await userFeatureFlagToggler.EnableAsync(command.FeatureFlagKey, tenant.Id, user.Id, now, cancellationToken);
            events.CollectEvent(new FeatureFlagUserOverrideSet(command.FeatureFlagKey, user.Id.ToString()));
        }
        else
        {
            await userFeatureFlagToggler.DisableAsync(command.FeatureFlagKey, tenant.Id, user.Id, now, cancellationToken);
            events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FeatureFlagKey, user.Id.ToString()));
        }

        tenant.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
