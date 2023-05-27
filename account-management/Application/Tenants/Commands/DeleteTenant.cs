using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class DeleteTenant
{
    public sealed record Command(TenantId Id) : IRequest<CommandResult<Tenant>>;

    public sealed class Handler : IRequestHandler<Command, CommandResult<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<CommandResult<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
            if (tenant is null)
            {
                return CommandResult<Tenant>.NotFound($"Tenant with id '{command.Id}' not found.");
            }

            _tenantRepository.Remove(tenant);

            return CommandResult<Tenant>.NoContent();
        }
    }
}