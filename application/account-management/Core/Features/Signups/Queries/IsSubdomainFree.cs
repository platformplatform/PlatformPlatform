using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Signups.Queries;

[PublicAPI]
public sealed record IsSubdomainFreeQuery(string Subdomain) : IRequest<Result<bool>>;

public sealed class IsSubdomainFreeHandler(ITenantRepository tenantRepository)
    : IRequestHandler<IsSubdomainFreeQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsSubdomainFreeQuery query, CancellationToken cancellationToken)
    {
        return await tenantRepository.IsSubdomainFreeAsync(query.Subdomain, cancellationToken);
    }
}
