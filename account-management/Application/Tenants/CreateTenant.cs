using FluentValidation;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public static class CreateTenant
{
    public sealed record Command(string Subdomain, string Name, string? Phone, string Email)
        : ICommand, ITenantValidation, IRequest<Result<TenantId>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<TenantId>>
    {
        private readonly ISender _mediator;
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository, ISender mediator)
        {
            _tenantRepository = tenantRepository;
            _mediator = mediator;
        }

        public async Task<Result<TenantId>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(command.Subdomain, command.Name, command.Phone);
            await _tenantRepository.AddAsync(tenant, cancellationToken);

            await CreateTenantOwnerAsync(tenant.Id, command.Email, cancellationToken);
            return tenant.Id;
        }

        private async Task CreateTenantOwnerAsync(TenantId tenantId, string tenantOwnerEmail,
            CancellationToken cancellationToken)
        {
            var createTenantOwnerUserCommand = new CreateUser.Command(tenantId, tenantOwnerEmail, UserRole.TenantOwner);
            var result = await _mediator.Send(createTenantOwnerUserCommand, cancellationToken);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to create a TenantOwner user for tenant. Reason: {result.GetErrorSummary()}");
            }
        }
    }

    [UsedImplicitly]
    public sealed class Validator : TenantValidator<Command>
    {
        public Validator(ITenantRepository tenantRepository)
        {
            RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
            RuleFor(x => x.Subdomain).NotEmpty();
            RuleFor(x => x.Subdomain)
                .Matches(@"^[a-z0-9]{3,30}$")
                .WithMessage("Subdomain must be between 3-30 alphanumeric and lowercase characters.")
                .MustAsync(async (subdomain, cancellationToken) =>
                    await tenantRepository.IsSubdomainFreeAsync(subdomain, cancellationToken))
                .WithMessage("The subdomain is not available.")
                .When(x => !string.IsNullOrEmpty(x.Subdomain));
        }
    }
}