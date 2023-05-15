using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.Application;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Queries;

/// <summary>
///     The GetTenantByIdQuery will retrieve a Tenant with the specified TenantId from the repository. The query
///     will be handled by <see cref="GetTenantQueryHandler" />. Returns the TenantDto if found, otherwise null.
/// </summary>
public sealed record GetTenantByIdQuery(TenantId Id) : IRequest<QueryResult<TenantDto>>;

public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantByIdQuery, QueryResult<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<QueryResult<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant == null
            ? QueryResult<TenantDto>.Failure($"Tenant with id '{request.Id.AsRawString()}' not found.")
            : tenant.Adapt<TenantDto>();
    }
}