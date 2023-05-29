using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Users.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class CreateTenant
{
    public sealed record Command(string Name, string Subdomain, string Email, string? Phone)
        : ICommand, ITenantValidation, IRequest<Result<Tenant>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<Tenant>>
    {
        private readonly ISender _mediator;
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository, ISender mediator)
        {
            _tenantRepository = tenantRepository;
            _mediator = mediator;
        }

        public async Task<Result<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(command.Name, command.Subdomain, command.Email, command.Phone);
            await _tenantRepository.AddAsync(tenant, cancellationToken);

            await CreateTenantOwner(command.Email, cancellationToken);
            return tenant;
        }

        private async Task CreateTenantOwner(string tenantOwnerEmail, CancellationToken cancellationToken)
        {
            var createTenantOwnerUserCommand = new CreateUser.Command(tenantOwnerEmail, UserRole.TenantOwner);
            var result = await _mediator.Send(createTenantOwnerUserCommand, cancellationToken);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to create a TenantOwner user for tenant. Reason: {result.ErrorMessage}");
            }
        }
    }

    [UsedImplicitly]
    public sealed class Validator : TenantValidator<Command>
    {
        public Validator(ITenantRepository repository, IUserRepository userRepository)
        {
            RuleFor(x => x.Subdomain).NotEmpty();
            RuleFor(x => x.Subdomain)
                .Length(3, 30).Matches(@"^[a-z0-9]+$")
                .WithMessage("Subdomain must be between 3-30 alphanumeric and lowercase characters.")
                .MustAsync(async (subdomain, token) => await repository.IsSubdomainFreeAsync(subdomain, token))
                .WithMessage("The subdomain is not available.")
                .When(x => !string.IsNullOrEmpty(x.Subdomain));

            RuleFor(x => x.Email)
                .MustAsync(async (email, token) => await userRepository.IsEmailFreeAsync(email, token))
                .WithMessage(x => $"The email '{x.Email}' is already in use by another user.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}