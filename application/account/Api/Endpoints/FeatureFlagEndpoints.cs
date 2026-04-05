using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
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

        routes.MapGet("/internal-api/account/feature-flags/{featureFlagKey}/tenants", async Task<ApiResult<GetFeatureFlagTenantsResponse>> (string featureFlagKey, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagTenantsQuery { FeatureFlagKey = featureFlagKey })
        ).Produces<GetFeatureFlagTenantsResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/activate", async Task<ApiResult> (string featureFlagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/deactivate", async Task<ApiResult> (string featureFlagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", async Task<ApiResult> (string featureFlagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", async Task<ApiResult> (string featureFlagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", async Task<ApiResult> (string featureFlagKey, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveTenantFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, TenantId = tenantId })
        ).DisableAntiforgery();

        routes.MapGet("/internal-api/account/feature-flags/{featureFlagKey}/users", async Task<ApiResult<GetFeatureFlagUsersResponse>> (string featureFlagKey, string? search, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagUsersQuery { FeatureFlagKey = featureFlagKey, Search = search })
        ).Produces<GetFeatureFlagUsersResponse>();

        routes.MapPut("/internal-api/account/feature-flags/{featureFlagKey}/user-override", async Task<ApiResult> (string featureFlagKey, SetUserFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        routes.MapDelete("/internal-api/account/feature-flags/{featureFlagKey}/user-override", async Task<ApiResult> (string featureFlagKey, UserId userId, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveUserFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, UserId = userId, TenantId = tenantId })
        ).DisableAntiforgery();

        // Authenticated API endpoints (tenant owner and user operations)
        var group = routes.MapGroup("/api/account/feature-flags").WithTags("FeatureFlags").RequireAuthorization().ProducesValidationProblem();

        group.MapPut("/{featureFlagKey}/tenant-override", async Task<ApiResult> (string featureFlagKey, SetTenantFeatureFlagOwnerCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        );

        group.MapPut("/{featureFlagKey}/user-override", async Task<ApiResult> (string featureFlagKey, SetUserFeatureFlagCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        );
    }
}
