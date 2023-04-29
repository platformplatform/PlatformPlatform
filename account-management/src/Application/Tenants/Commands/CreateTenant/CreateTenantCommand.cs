using MediatR;
using PlatformPlatform.AccountManagement.Application.Shared;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenantCommandHandler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkBehavior.
/// </summary>
public sealed record CreateTenantCommand(string Name, string Subdomain, string Email, string Phone)
    : ITenantCommand, IRequest<Result<TenantDto>>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantDto>> Handle(CreateTenantCommand createTenantCommand,
        CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            Name = createTenantCommand.Name,
            Subdomain = createTenantCommand.Subdomain,
            Email = createTenantCommand.Email,
            Phone = createTenantCommand.Phone
        };
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        var tenantDto = TenantDto.CreateFrom(tenant);
        return Result<TenantDto>.Success(tenantDto);
    }
}