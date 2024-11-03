using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Queries;

[PublicAPI]
public sealed record GetTenantQuery(TenantId Id) : IRequest<Result<TenantResponse>>;

[PublicAPI]
public sealed record TenantResponse(TenantId Id, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt, string Name, TenantState State);

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant?.Adapt<TenantResponse>() ?? Result<TenantResponse>.NotFound($"Tenant with id '{request.Id}' not found.");
    }
}
