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

        routes.MapGet("/internal-api/account/feature-flags/{flagKey}/tenants", async Task<ApiResult<GetFlagTenantsResponse>> (string flagKey, IMediator mediator)
            => await mediator.Send(new GetFlagTenantsQuery { FlagKey = flagKey })
        ).Produces<GetFlagTenantsResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/activate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(flagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/deactivate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(flagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/rollout-percentage", async Task<ApiResult> (string flagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, long tenantId, IMediator mediator)
            => await mediator.Send(new RemoveTenantFeatureFlagOverrideCommand { FlagKey = flagKey, TenantId = tenantId })
        ).DisableAntiforgery();

        routes.MapGet("/internal-api/account/feature-flags/{flagKey}/users", async Task<ApiResult<GetFlagUsersResponse>> (string flagKey, IMediator mediator)
            => await mediator.Send(new GetFlagUsersQuery { FlagKey = flagKey })
        ).Produces<GetFlagUsersResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{flagKey}/user-override", async Task<ApiResult> (string flagKey, SetUserFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{flagKey}/user-override", async Task<ApiResult> (string flagKey, string userId, long tenantId, IMediator mediator)
            => await mediator.Send(new RemoveUserFeatureFlagOverrideCommand { FlagKey = flagKey, UserId = userId, TenantId = tenantId })
        ).DisableAntiforgery();

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
