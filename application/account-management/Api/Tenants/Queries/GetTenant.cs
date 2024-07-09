using Mapster;
using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Queries;

public sealed class GetTenantEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Tenants");

        group.MapGet("/{id}", async Task<ApiResult<TenantResponseDto>> ([AsParameters] GetTenantQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<TenantResponseDto>();
    }
}

public sealed record GetTenantQuery(TenantId Id) : IRequest<Result<TenantResponseDto>>;

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantQuery, Result<TenantResponseDto>>
{
    public async Task<Result<TenantResponseDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        return tenant?.Adapt<TenantResponseDto>() ?? Result<TenantResponseDto>.NotFound($"Tenant with id '{request.Id}' not found.");
    }
}
