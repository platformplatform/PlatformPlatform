using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Account.Features.Tenants.Queries;

[PublicAPI]
public sealed record GetCurrentTenantQuery : IRequest<Result<TenantResponse>>;

[PublicAPI]
public sealed record TenantResponse(
    TenantId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    TenantState State,
    SuspensionReason? SuspensionReason,
    string? LogoUrl
);

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetCurrentTenantQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        return tenant.Adapt<TenantResponse>();
    }
}
