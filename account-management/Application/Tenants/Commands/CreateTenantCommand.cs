using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record CreateTenantCommand(string Name) : IRequest<long>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, long>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<long> Handle(CreateTenantCommand createTenantCommand, CancellationToken cancellationToken)
    {
        var tenant = new Tenant {Name = createTenantCommand.Name};
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        return tenant.Id;
    }
}