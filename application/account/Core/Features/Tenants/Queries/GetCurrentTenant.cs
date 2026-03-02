using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using Mapster;
using SharedKernel.Authentication;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.Queries;

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
