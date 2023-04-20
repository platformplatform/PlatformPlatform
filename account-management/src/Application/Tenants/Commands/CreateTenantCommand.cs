using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record CreateTenantCommand(string Name) : IRequest<TenantResponseDto>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantResponseDto>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantResponseDto> Handle(CreateTenantCommand createTenantCommand,
        CancellationToken cancellationToken)
    {
        var tenant = new Tenant {Name = createTenantCommand.Name};
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        return TenantResponseDto.CreateFrom(tenant);
    }
}