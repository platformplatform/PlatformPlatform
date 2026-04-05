using Account.Features.FeatureFlags.Shared;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Commands;

[PublicAPI]
public sealed record SetUserFeatureFlagCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public string FeatureFlagKey { get; init; } = null!;

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

public sealed class SetUserFeatureFlagHandler(UserFeatureFlagToggler userFeatureFlagToggler, ITenantRepository tenantRepository, IExecutionContext executionContext, TimeProvider timeProvider, ITelemetryEventsCollector events)
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

        var tenantId = executionContext.TenantId!;
        var userId = executionContext.UserInfo.Id!;
        var now = timeProvider.GetUtcNow();

        if (command.Enabled)
        {
            await userFeatureFlagToggler.EnableAsync(command.FeatureFlagKey, tenantId, userId, now, cancellationToken);
            events.CollectEvent(new FeatureFlagUserOverrideSet(command.FeatureFlagKey, userId.ToString()));
        }
        else
        {
            await userFeatureFlagToggler.DisableAsync(command.FeatureFlagKey, tenantId, userId, now, cancellationToken);
            events.CollectEvent(new FeatureFlagUserOverrideRemoved(command.FeatureFlagKey, userId.ToString()));
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        tenant!.IncrementFeatureFlagVersion();
        tenantRepository.Update(tenant);

        return Result.Success();
    }
}
