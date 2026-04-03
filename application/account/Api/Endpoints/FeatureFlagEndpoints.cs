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
        // Internal API endpoints (back-office operations, no auth group)
        var internalGroup = routes.MapGroup("/internal-api/account/feature-flags").WithGroupName(OpenApiDocumentNames.Account);

        internalGroup.MapGet("/", async Task<ApiResult<GetFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetFeatureFlagsQuery())
        ).Produces<GetFeatureFlagsResponse>();

        internalGroup.MapGet("/{flagKey}/tenants", async Task<ApiResult<GetFlagTenantsResponse>> (string flagKey, IMediator mediator)
            => await mediator.Send(new GetFlagTenantsQuery { FlagKey = flagKey })
        ).Produces<GetFlagTenantsResponse>();

        internalGroup.MapPut("/{flagKey}/activate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(flagKey))
        ).DisableAntiforgery();

        internalGroup.MapPut("/{flagKey}/deactivate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(flagKey))
        ).DisableAntiforgery();

        internalGroup.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).DisableAntiforgery();

        internalGroup.MapPut("/{flagKey}/rollout-percentage", async Task<ApiResult> (string flagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).DisableAntiforgery();

        // Authenticated API endpoints (tenant owner and user operations)
        var group = routes.MapGroup("/api/account/feature-flags").WithTags("FeatureFlags").WithGroupName(OpenApiDocumentNames.Account).RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/tenant-configurable", async Task<ApiResult<TenantConfigurableFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantConfigurableFeatureFlagsQuery())
        ).Produces<TenantConfigurableFeatureFlagsResponse>();

        group.MapGet("/user-configurable", async Task<ApiResult<UserConfigurableFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetUserConfigurableFeatureFlagsQuery())
        ).Produces<UserConfigurableFeatureFlagsResponse>();

        group.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagOwnerCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );

        group.MapPut("/{flagKey}/user-override", async Task<ApiResult> (string flagKey, SetUserFeatureFlagCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );
    }
}
