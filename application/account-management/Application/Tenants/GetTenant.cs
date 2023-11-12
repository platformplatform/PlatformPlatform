using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record GetTenantQuery(TenantId Id) : IRequest<Result<TenantResponseDto>>;

[UsedImplicitly]
public sealed class GetTenantHandler : IRequestHandler<GetTenantQuery, Result<TenantResponseDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantResponseDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant?.Adapt<TenantResponseDto>()
               ?? Result<TenantResponseDto>.NotFound($"Tenant with id '{request.Id}' not found.");
    }
}