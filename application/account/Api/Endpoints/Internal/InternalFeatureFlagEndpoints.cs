using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints.Internal;

public sealed class InternalFeatureFlagEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/internal-api/account/feature-flags", async Task<ApiResult<GetFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetFeatureFlagsQuery())
        ).Produces<GetFeatureFlagsResponse>();

        routes.MapGet("/internal-api/account/feature-flags/{featureFlagKey}/tenants", async Task<ApiResult<GetFeatureFlagTenantsResponse>> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagTenantsQuery { FeatureFlagKey = featureFlagKey })
        ).Produces<GetFeatureFlagTenantsResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/activate", async Task<ApiResult> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/deactivate", async Task<ApiResult> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveTenantFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, TenantId = tenantId })
        ).DisableAntiforgery();

        routes.MapGet("/internal-api/account/feature-flags/{featureFlagKey}/users", async Task<ApiResult<GetFeatureFlagUsersResponse>> (FeatureFlagKey featureFlagKey, string? search, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagUsersQuery { FeatureFlagKey = featureFlagKey, Search = search })
        ).Produces<GetFeatureFlagUsersResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/user-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetUserFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{featureFlagKey}/user-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, UserId userId, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveUserFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, UserId = userId, TenantId = tenantId })
        ).DisableAntiforgery();
    }
}
