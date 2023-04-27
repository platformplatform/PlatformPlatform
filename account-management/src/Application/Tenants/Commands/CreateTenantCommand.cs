using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenantCommandHandler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkBehavior.
/// </summary>
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
        return TenantDto.CreateFrom(tenant);
    }
}