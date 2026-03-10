using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class FeatureFlagEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        // Internal API endpoints (back-office operations, no auth group)
        routes.MapGet("/internal-api/account/feature-flags", async Task<ApiResult<GetFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetFeatureFlagsQuery())
        ).Produces<GetFeatureFlagsResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/activate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(flagKey))
        );

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/deactivate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(flagKey))
        );

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/rollout-percentage", async Task<ApiResult> (string flagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );

        // Authenticated API endpoints (tenant owner and user operations)
        var group = routes.MapGroup("/api/account/feature-flags").WithTags("FeatureFlags").RequireAuthorization().ProducesValidationProblem();

        group.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagOwnerCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );

        group.MapPut("/{flagKey}/user-override", async Task<ApiResult> (string flagKey, SetUserFeatureFlagCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );
    }
}
