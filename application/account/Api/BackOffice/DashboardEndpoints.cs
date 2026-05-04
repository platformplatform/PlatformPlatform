using Account.Features.BackOffice.Dashboard.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class DashboardEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/dashboard";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeDashboard")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/kpis", async Task<ApiResult<BackOfficeDashboardKpisResponse>> ([AsParameters] GetDashboardKpisQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<BackOfficeDashboardKpisResponse>();

        group.MapGet("/trends", async Task<ApiResult<BackOfficeDashboardTrendsResponse>> ([AsParameters] GetDashboardTrendsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<BackOfficeDashboardTrendsResponse>();
    }
}
