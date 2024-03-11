using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.AccountRegistrations;

[UsedImplicitly]
public sealed record IsSubdomainFreeQuery(string Subdomain) : IRequest<Result<bool>>;

[UsedImplicitly]
public sealed class IsSubdomainFreeHandler(ITenantRepository tenantRepository)
    : IRequestHandler<IsSubdomainFreeQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsSubdomainFreeQuery request, CancellationToken cancellationToken)
    {
        return await tenantRepository.IsSubdomainFreeAsync(request.Subdomain, cancellationToken);
    }
}