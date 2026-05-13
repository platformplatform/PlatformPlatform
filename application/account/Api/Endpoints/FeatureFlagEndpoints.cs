using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.Endpoints;

public sealed class FeatureFlagEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/account/feature-flags").WithTags("FeatureFlags").WithGroupName(OpenApiDocumentNames.Account).RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/tenant-configurable", async Task<ApiResult<TenantConfigurableFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantConfigurableFeatureFlagsQuery())
        ).Produces<TenantConfigurableFeatureFlagsResponse>();

        group.MapGet("/user-configurable", async Task<ApiResult<UserConfigurableFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetUserConfigurableFeatureFlagsQuery())
        ).Produces<UserConfigurableFeatureFlagsResponse>();

        group.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagOwnerCommand command, IMediator mediator)
            => (await mediator.Send(command with { FlagKey = flagKey })).AddRefreshAuthenticationTokens()
        );

        group.MapPut("/{flagKey}/user-override", async Task<ApiResult> (string flagKey, SetUserFeatureFlagCommand command, IMediator mediator)
            => (await mediator.Send(command with { FlagKey = flagKey })).AddRefreshAuthenticationTokens()
        );
    }
}
