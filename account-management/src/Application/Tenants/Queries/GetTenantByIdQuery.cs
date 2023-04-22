using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Queries;

public sealed record GetTenantByIdQuery(TenantId Id) : IRequest<TenantDto?>;

/// <summary>
///     Handles the GetTenantByIdQuery by retrieving a Tenant with the specified TenantId from the repository.
///     Returns the TenantDto if found, otherwise null.
/// </summary>
public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return TenantDto.CreateFrom(tenant);
    }
}