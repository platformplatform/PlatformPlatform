using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class CreateTenant
{
    public sealed record Command(string Name, string Subdomain, string Email, string? Phone)
        : ICommand, ITenantValidation, IRequest<Result<Tenant>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(command.Name, command.Subdomain, command.Email, command.Phone);
            await _tenantRepository.AddAsync(tenant, cancellationToken);
            return tenant;
        }
    }

    [UsedImplicitly]
    public sealed class Validator : TenantValidator<Command>
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