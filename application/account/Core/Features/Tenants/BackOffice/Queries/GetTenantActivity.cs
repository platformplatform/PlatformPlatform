using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantActivityQuery(TenantId Id) : IRequest<Result<TenantActivityResponse>>;

[PublicAPI]
public sealed record TenantActivityResponse(TenantActivityEvent[] Events);

[PublicAPI]
public sealed record TenantActivityEvent(DateTimeOffset Timestamp, string Name, string? Description);

public sealed class GetTenantActivityHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantActivityQuery, Result<TenantActivityResponse>>
{
    public async Task<Result<TenantActivityResponse>> Handle(GetTenantActivityQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantActivityResponse>.NotFound($"Tenant with id '{query.Id}' was not found.");
        }

        // Activity is sourced from Application Insights telemetry scoped by tenant id. The Application Insights
        // wiring is delivered separately; until then this endpoint returns an empty list so the front-end can
        // render the activity tab without a hard dependency on the telemetry pipeline.
        return new TenantActivityResponse([]);
    }
}
