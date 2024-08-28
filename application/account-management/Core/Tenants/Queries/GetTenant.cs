using Mapster;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Core.Tenants.Queries;

public sealed record GetTenantQuery(TenantId Id) : IRequest<Result<TenantResponseDto>>;

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantQuery, Result<TenantResponseDto>>
{
    public async Task<Result<TenantResponseDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant?.Adapt<TenantResponseDto>() ?? Result<TenantResponseDto>.NotFound($"Tenant with id '{request.Id}' not found.");
    }
}
