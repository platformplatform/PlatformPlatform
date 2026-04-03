using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.Queries;

[PublicAPI]
public sealed record GetTenantsQuery : IRequest<Result<GetTenantsResponse>>;

[PublicAPI]
public sealed record GetTenantsResponse(TenantSummary[] Tenants);

[PublicAPI]
public sealed record TenantSummary(TenantId Id, string Name);

public sealed class GetTenantsHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantsQuery, Result<GetTenantsResponse>>
{
    public async Task<Result<GetTenantsResponse>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);

        var tenantSummaries = tenants.Select(t => new TenantSummary(t.Id, t.Name)).ToArray();

        return new GetTenantsResponse(tenantSummaries);
    }
}
