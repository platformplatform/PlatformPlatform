using Account.Features.BackOffice.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class BackOfficeEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        // BackOffice:Host is required (validated at startup via ValidateOnStart in
        // ApiDependencyConfiguration.AddBackOfficeHostOptions). The startup validation must stay in place
        // so a missing/blank value fails loudly rather than silently 404-ing back-office endpoints.
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOffice")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/me", async Task<ApiResult<MeResponse>> ([AsParameters] GetMeQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<MeResponse>();
    }
}
