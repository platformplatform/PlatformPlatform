using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenant.Handler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkPipelineBehavior.
/// </summary>
public static class CreateTenant
{
    public sealed record Command(string Name, string Subdomain, string Email, string? Phone)
        : ITenantValidation, IRequest<CommandResult<Tenant>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, CommandResult<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public Task<CommandResult<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(command.Name, command.Subdomain, command.Email, command.Phone);

            _tenantRepository.Add(tenant);

            return CommandResult<Tenant>.Created(tenant);
        }

        [UsedImplicitly]
        public sealed class Validator : TenantValidatorBase<Command>
        {
            public Validator(ITenantRepository repository)
            {
                RuleFor(x => x.Subdomain).NotEmpty();
                RuleFor(x => x.Subdomain)
                    .Length(3, 30).Matches(@"^[a-z0-9]+$")
                    .WithMessage("Subdomain must be between 3-30 alphanumeric and lowercase characters.")
                    .MustAsync(async (subdomain, token) => await repository.IsSubdomainFreeAsync(subdomain, token))
                    .WithMessage("The subdomain is not available.")
                    .When(x => !string.IsNullOrEmpty(x.Subdomain));
            }
        }
    }
}