using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record CreateTenantCommand(string Name) : IRequest<TenantDto>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand createTenantCommand,
        CancellationToken cancellationToken)
    {
        var tenant = new Tenant {Name = createTenantCommand.Name};
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        return TenantDto.CreateFrom(tenant)!;
    }
}