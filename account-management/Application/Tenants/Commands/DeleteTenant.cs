using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class DeleteTenant
{
    public sealed record Command(TenantId Id) : ICommand, IRequest<Result<Tenant>>;

    public sealed class Handler : IRequestHandler<Command, Result<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
            if (tenant is null)
            {
                return Result<Tenant>.NotFound($"Tenant with id '{command.Id}' not found.");
            }

            _tenantRepository.Remove(tenant);
            return Result<Tenant>.NoContent();
        }
    }
}