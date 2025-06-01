using PlatformPlatform.AccountManagement.Features.Addresses.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class AddressEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/addresses";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Addresses").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/search", async Task<ApiResult<SearchAddressesResponse>> ([AsParameters] SearchAddressesQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<SearchAddressesResponse>();
    }
}
