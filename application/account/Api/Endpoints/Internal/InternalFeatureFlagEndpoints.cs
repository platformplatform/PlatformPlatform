using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints.Internal;

public sealed class InternalFeatureFlagEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/internal-api/account/feature-flags";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("InternalFeatureFlags").ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<GetFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetFeatureFlagsQuery())
        ).Produces<GetFeatureFlagsResponse>();

        group.MapGet("/{featureFlagKey}/tenants", async Task<ApiResult<GetFeatureFlagTenantsResponse>> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagTenantsQuery { FeatureFlagKey = featureFlagKey })
        ).Produces<GetFeatureFlagTenantsResponse>();

        group.MapPut("/{featureFlagKey}/activate", async Task<ApiResult> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/deactivate", async Task<ApiResult> (FeatureFlagKey featureFlagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(featureFlagKey))
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/tenant-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/rollout-percentage", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        group.MapDelete("/{featureFlagKey}/tenant-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveTenantFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, TenantId = tenantId })
        ).DisableAntiforgery();

        group.MapGet("/{featureFlagKey}/users", async Task<ApiResult<GetFeatureFlagUsersResponse>> (FeatureFlagKey featureFlagKey, string? search, IMediator mediator)
            => await mediator.Send(new GetFeatureFlagUsersQuery { FeatureFlagKey = featureFlagKey, Search = search })
        ).Produces<GetFeatureFlagUsersResponse>();

        group.MapPut("/{featureFlagKey}/user-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetUserFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        ).DisableAntiforgery();

        group.MapDelete("/{featureFlagKey}/user-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, UserId userId, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveUserFeatureFlagOverrideCommand { FeatureFlagKey = featureFlagKey, UserId = userId, TenantId = tenantId })
        ).DisableAntiforgery();
    }
}
