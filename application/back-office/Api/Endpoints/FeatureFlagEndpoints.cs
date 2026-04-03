using BackOffice.Features.FeatureFlags;
using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;

namespace BackOffice.Api.Endpoints;

public sealed class FeatureFlagEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/feature-flags";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("FeatureFlags").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async (AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFeatureFlagsAsync(cancellationToken));
            }
        );

        group.MapGet("/{flagKey}/tenants", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFlagTenantsAsync(flagKey, cancellationToken));
            }
        );

        group.MapPut("/{flagKey}/activate", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.ActivateFlagAsync(flagKey, cancellationToken));
            }
        );

        group.MapPut("/{flagKey}/deactivate", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.DeactivateFlagAsync(flagKey, cancellationToken));
            }
        );

        group.MapPut("/{flagKey}/tenant-override", async (string flagKey, SetTenantOverrideRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetTenantOverrideAsync(flagKey, request.TenantId, request.Enabled, cancellationToken));
            }
        );

        group.MapPut("/{flagKey}/rollout-percentage", async (string flagKey, SetRolloutPercentageRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetRolloutPercentageAsync(flagKey, request.RolloutPercentage, cancellationToken));
            }
        );
    }

    private static async Task<IResult> ProxyResponse(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Results.Content(content, contentType, statusCode: (int)response.StatusCode);
    }
}

public sealed record SetTenantOverrideRequest(long TenantId, bool Enabled);

public sealed record SetRolloutPercentageRequest(int RolloutPercentage);
