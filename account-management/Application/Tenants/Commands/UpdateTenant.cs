using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class UpdateTenant
{
    public sealed record Command(TenantId Id, string Name, string Email, string? Phone)
        : ITenantValidation, IRequest<Result<Tenant>>;

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

            tenant.Update(command.Name, command.Email, command.Phone);

            _tenantRepository.Update(tenant);

            return tenant;
        }

        [UsedImplicitly]
        public sealed class Validator : TenantValidator<Command>
        {
        }
    }
}