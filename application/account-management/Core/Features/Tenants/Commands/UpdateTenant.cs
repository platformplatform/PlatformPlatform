using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record UpdateTenantCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public TenantId Id { get; init; } = null!;

    public required string Name { get; init; }
}

public sealed class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Name).Length(1, 30)
            .WithMessage("Name must be between 1 and 30 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));
    }
}

public sealed class UpdateTenantHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateTenantCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        tenant.Update(command.Name);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantUpdated());

        return Result.Success();
    }
}
