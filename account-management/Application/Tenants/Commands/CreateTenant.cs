using System.Net;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenant.Handler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkPipelineBehavior.
/// </summary>
public static class CreateTenant
{
    public sealed record Command(string Name, string Subdomain, string Email, string? Phone)
        : IRequest<CommandResult<Tenant>>;

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

            return Task.FromResult(CommandResult<Tenant>.Success(tenant, HttpStatusCode.Created));
        }

        [UsedImplicitly]
        public sealed class Validator : AbstractValidator<Command>
        {
            private const string SubdomainUniqueErrorMessage = "The subdomain must be unique.";
            private const string SubdomainRuleErrorMessage = "Subdomain must be alphanumeric and lowercase.";

            private readonly ITenantRepository _tenantRepository;

            public Validator(ITenantRepository tenantRepository)
            {
                _tenantRepository = tenantRepository;

                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Name).Length(1, 30).When(x => !string.IsNullOrEmpty(x.Name));
                RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
                RuleFor(x => x.Phone).SetValidator(new SharedValidations.Phone());
                RuleFor(x => x.Subdomain).NotEmpty();
                RuleFor(x => x.Subdomain)
                    .Length(3, 30)
                    .Matches(@"^[a-z0-9]+$").WithMessage(SubdomainRuleErrorMessage)
                    .MustAsync(SubdomainMustBeAvailable)
                    .WithMessage(SubdomainUniqueErrorMessage)
                    .When(x => !string.IsNullOrEmpty(x.Subdomain));
            }

            private async Task<bool> SubdomainMustBeAvailable(string subdomain, CancellationToken cancellation)
            {
                return await _tenantRepository.IsSubdomainFreeAsync(subdomain, cancellation);
            }
        }
    }
}