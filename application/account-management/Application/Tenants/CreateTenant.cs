using FluentValidation;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Tracking;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record CreateTenantCommand(string Subdomain, string Name, string? Phone, string Email)
    : ICommand, ITenantValidation, IRequest<Result<TenantId>>;

[UsedImplicitly]
public sealed class CreateTenantHandler(
    ITenantRepository tenantRepository,
    IAnalyticEventsCollector analyticEventsCollector,
    ISender mediator
)
    : IRequestHandler<CreateTenantCommand, Result<TenantId>>
{
    public async Task<Result<TenantId>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(command.Subdomain, command.Name, command.Phone);
        await tenantRepository.AddAsync(tenant, cancellationToken);
        analyticEventsCollector.CollectEvent(
            "TenantCreated",
            new Dictionary<string, string>
            {
                { "Tenant_Id", tenant.Id.ToString() },
                { "Event_TenantState", tenant.State.ToString() }
            }
        );

        await CreateTenantOwnerAsync(tenant.Id, command.Email, cancellationToken);

        return tenant.Id;
    }

    private async Task CreateTenantOwnerAsync(
        TenantId tenantId,
        string tenantOwnerEmail,
        CancellationToken cancellationToken
    )
    {
        var createTenantOwnerUserCommand = new CreateUserCommand(tenantId, tenantOwnerEmail, UserRole.TenantOwner);
        var result = await mediator.Send(createTenantOwnerUserCommand, cancellationToken);

        if (!result.IsSuccess) throw new UnreachableException($"Create Tenant Owner: {result.GetErrorSummary()}");
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