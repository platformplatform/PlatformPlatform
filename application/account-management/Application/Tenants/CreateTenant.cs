using FluentValidation;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record CreateTenantCommand(string Subdomain, string Name, string? Phone, string Email)
    : ICommand, ITenantValidation, IRequest<Result<TenantId>>;

[UsedImplicitly]
public sealed class CreateTenantHandler(
    ITenantRepository tenantRepository,
    ITelemetryEventsCollector events
) : IRequestHandler<CreateTenantCommand, Result<TenantId>>
{
    public async Task<Result<TenantId>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(command.Subdomain, command.Name, command.Phone, command.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State));

        return tenant.Id;
    }
}

[UsedImplicitly]
public sealed class CreateTenantValidator : TenantValidator<CreateTenantCommand>
{
    public CreateTenantValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.Subdomain).NotEmpty();
        RuleFor(x => x.Subdomain)
            .Matches("^[a-z0-9]{3,30}$")
            .WithMessage("Subdomain must be between 3-30 alphanumeric and lowercase characters.")
            .MustAsync(tenantRepository.IsSubdomainFreeAsync)
            .WithMessage("The subdomain is not available.")
            .When(x => !string.IsNullOrEmpty(x.Subdomain));
    }
}