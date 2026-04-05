using Account.Features.FeatureFlags.Commands;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class FeatureFlagEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/account/feature-flags").WithTags("FeatureFlags").RequireAuthorization().ProducesValidationProblem();

        group.MapPut("/{featureFlagKey}/tenant-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetTenantFeatureFlagOwnerCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        );

        group.MapPut("/{featureFlagKey}/user-override", async Task<ApiResult> (FeatureFlagKey featureFlagKey, SetUserFeatureFlagCommand command, IMediator mediator)
            => await mediator.Send(command with { FeatureFlagKey = featureFlagKey })
        );
    }
}
