using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Domain;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class FeatureFlagEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/feature-flags";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeFeatureFlags")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<GetFeatureFlagsResponse>> ([AsParameters] GetFeatureFlagsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<GetFeatureFlagsResponse>();

        group.MapGet("/{flagKey}/tenants", async Task<ApiResult<GetFeatureFlagTenantsResponse>> (string flagKey, [AsParameters] GetFeatureFlagTenantsQuery query, IMediator mediator)
            => await mediator.Send(query with { FlagKey = flagKey })
        ).Produces<GetFeatureFlagTenantsResponse>();

        group.MapPut("/{flagKey}/activate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(flagKey))
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapPut("/{flagKey}/deactivate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(flagKey))
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapDelete("/{flagKey}", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeleteFeatureFlagCommand(flagKey))
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapPut("/{flagKey}/rollout-percentage", async Task<ApiResult> (string flagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapDelete("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveTenantFeatureFlagOverrideCommand { FlagKey = flagKey, TenantId = tenantId })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapGet("/{flagKey}/users", async Task<ApiResult<GetFeatureFlagUsersResponse>> (string flagKey, [AsParameters] GetFeatureFlagUsersQuery query, IMediator mediator)
            => await mediator.Send(query with { FlagKey = flagKey })
        ).Produces<GetFeatureFlagUsersResponse>();

        group.MapPut("/{flagKey}/user-override", async Task<ApiResult> (string flagKey, SetUserFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapDelete("/{flagKey}/user-override", async Task<ApiResult> (string flagKey, UserId userId, TenantId tenantId, IMediator mediator)
            => await mediator.Send(new RemoveUserFeatureFlagOverrideCommand { FlagKey = flagKey, UserId = userId, TenantId = tenantId })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);
    }
}
