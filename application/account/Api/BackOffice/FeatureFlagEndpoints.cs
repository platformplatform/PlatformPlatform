using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
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

        group.MapGet("/", async Task<ApiResult<GetFeatureFlagsResponse>> (IMediator mediator)
            => await mediator.Send(new GetFeatureFlagsQuery())
        ).Produces<GetFeatureFlagsResponse>();

        group.MapGet("/{flagKey}/tenants", async Task<ApiResult<GetFlagTenantsResponse>> (string flagKey, IMediator mediator)
            => await mediator.Send(new GetFlagTenantsQuery { FlagKey = flagKey })
        ).Produces<GetFlagTenantsResponse>();

        group.MapPut("/{flagKey}/activate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new ActivateFeatureFlagCommand(flagKey))
        );

        group.MapPut("/{flagKey}/deactivate", async Task<ApiResult> (string flagKey, IMediator mediator)
            => await mediator.Send(new DeactivateFeatureFlagCommand(flagKey))
        );

        group.MapPut("/{flagKey}/tenant-override", async Task<ApiResult> (string flagKey, SetTenantFeatureFlagInternalCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );

        group.MapPut("/{flagKey}/rollout-percentage", async Task<ApiResult> (string flagKey, SetFeatureFlagRolloutPercentageCommand command, IMediator mediator)
            => await mediator.Send(command with { FlagKey = flagKey })
        );
    }
}
