using Account.Features.FeatureFlags.Queries;
using Account.Features.Users.BackOffice.Commands;
using Account.Features.Users.BackOffice.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Domain;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class UsersEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeUsers")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<BackOfficeUsersResponse>> ([AsParameters] GetBackOfficeUsersQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<BackOfficeUsersResponse>();

        group.MapGet("/{id}", async Task<ApiResult<BackOfficeUserDetailResponse>> (UserId id, IMediator mediator)
            => await mediator.Send(new GetBackOfficeUserDetailQuery(id))
        ).Produces<BackOfficeUserDetailResponse>();

        group.MapGet("/{id}/sessions", async Task<ApiResult<BackOfficeUserSessionsResponse>> (UserId id, [AsParameters] GetBackOfficeUserSessionsQuery query, IMediator mediator)
            => await mediator.Send(query with { Id = id })
        ).Produces<BackOfficeUserSessionsResponse>();

        group.MapGet("/{id}/login-history", async Task<ApiResult<BackOfficeUserLoginHistoryResponse>> (UserId id, [AsParameters] GetBackOfficeUserLoginHistoryQuery query, IMediator mediator)
            => await mediator.Send(query with { Id = id })
        ).Produces<BackOfficeUserLoginHistoryResponse>();

        group.MapGet("/{id}/feature-flags", async Task<ApiResult<GetUserFeatureFlagsResponse>> (UserId id, IMediator mediator)
            => await mediator.Send(new GetUserFeatureFlagsQuery { UserId = id })
        ).Produces<GetUserFeatureFlagsResponse>();

        group.MapPut("/{id}/ab-inclusion-pin", async Task<ApiResult> (UserId id, SetUserAbInclusionPinCommand command, IMediator mediator)
            => await mediator.Send(command with { UserId = id })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);
    }
}
