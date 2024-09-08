using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Tenants.Queries;

[PublicAPI]
public sealed record GetTenantQuery(TenantId Id) : IRequest<Result<TenantResponseDto>>;

[PublicAPI]
public sealed record TenantResponseDto(string Id, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt, string Name, TenantState State);

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantQuery, Result<TenantResponseDto>>
{
    public async Task<Result<TenantResponseDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant?.Adapt<TenantResponseDto>() ?? Result<TenantResponseDto>.NotFound($"Tenant with id '{request.Id}' not found.");
    }
}
