using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantUserCountsQuery(TenantId Id) : IRequest<Result<TenantUserCountsResponse>>;

[PublicAPI]
public sealed record TenantUserCountsResponse(int TotalUsers, int ActiveUsers, int PendingUsers);

public sealed class GetTenantUserCountsHandler(ITenantRepository tenantRepository, IUserRepository userRepository, TimeProvider timeProvider)
    : IRequestHandler<GetTenantUserCountsQuery, Result<TenantUserCountsResponse>>
{
    private const int ActiveWindowDays = 30;

    public async Task<Result<TenantUserCountsResponse>> Handle(GetTenantUserCountsQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantUserCountsResponse>.NotFound($"Tenant with id '{query.Id}' was not found.");
        }

        var activeSince = timeProvider.GetUtcNow().AddDays(-ActiveWindowDays);
        var (totalUsers, activeUsers, pendingUsers) = await userRepository.GetUserCountsForTenantUnfilteredAsync(tenant.Id, activeSince, cancellationToken);
        return new TenantUserCountsResponse(totalUsers, activeUsers, pendingUsers);
    }
}
