using Account.Features.BackOffice.BillingDrift.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class BillingDriftEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/billing-drift";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeBillingDrift")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/summary", async Task<ApiResult<BillingDriftSummaryResponse>> ([AsParameters] GetBillingDriftSummaryQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<BillingDriftSummaryResponse>();

        group.MapGet("/unsynced-summary", async Task<ApiResult<UnsyncedSubscriptionsSummaryResponse>> ([AsParameters] GetUnsyncedSubscriptionsSummaryQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UnsyncedSubscriptionsSummaryResponse>();

        group.MapGet("/mrr-consistency-summary", async Task<ApiResult<DashboardMrrConsistencySummaryResponse>> ([AsParameters] GetDashboardMrrConsistencySummaryQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<DashboardMrrConsistencySummaryResponse>();
    }
}
