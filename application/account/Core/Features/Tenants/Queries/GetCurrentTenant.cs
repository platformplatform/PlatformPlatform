using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Authentication;
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
    string? LogoUrl
);

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetCurrentTenantQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result<TenantResponse>.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        return tenant.Adapt<TenantResponse>();
    }
}
