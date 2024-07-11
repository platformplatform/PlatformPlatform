using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations.Queries;

public sealed record IsSubdomainFreeQuery(string Subdomain) : IRequest<Result<bool>>;

public sealed class IsSubdomainFreeRegistrationsEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/account-registrations";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("AccountRegistrations");

        group.MapGet("/is-subdomain-free", async Task<ApiResult<bool>> ([AsParameters] IsSubdomainFreeQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<bool>();
    }
}

public sealed class IsSubdomainFreeHandler(TenantRepository tenantRepository)
    : IRequestHandler<IsSubdomainFreeQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(IsSubdomainFreeQuery request, CancellationToken cancellationToken)
    {
        return await tenantRepository.IsSubdomainFreeAsync(request.Subdomain, cancellationToken);
    }
}
