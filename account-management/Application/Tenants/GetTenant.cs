using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public static class GetTenant
{
    public sealed record Query(TenantId Id) : IRequest<Result<TenantResponseDto>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Query, Result<TenantResponseDto>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantResponseDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
            return tenant?.Adapt<TenantResponseDto>()
                   ?? Result<TenantResponseDto>.NotFound($"Tenant with id '{request.Id}' not found.");
        }
    }
}