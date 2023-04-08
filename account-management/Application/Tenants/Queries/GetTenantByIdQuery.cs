using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Queries;

public sealed record GetTenantByIdQuery(long Id) : IRequest<TenantResponseDto?>;

public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantResponseDto?>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantResponseDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return TenantResponseDto.CreateFrom(tenant);
    }
}