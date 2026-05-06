using Account.Features.BackOffice.BillingEvents.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class BillingEventsEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/billing-events";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeBillingEvents")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<BillingEventsResponse>> ([AsParameters] GetBackOfficeBillingEventsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<BillingEventsResponse>();
    }
}
