using BackOffice.Features.FeatureFlags;
using SharedKernel.Domain;
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
        ).Produces<GetFeatureFlagsResponse>();

        group.MapGet("/{featureFlagKey}/tenants", async (string featureFlagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFeatureFlagTenantsAsync(featureFlagKey, cancellationToken));
            }
        ).Produces<GetFeatureFlagTenantsResponse>();

        group.MapPut("/{featureFlagKey}/activate", async (string featureFlagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.ActivateFeatureFlagAsync(featureFlagKey, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/deactivate", async (string featureFlagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.DeactivateFeatureFlagAsync(featureFlagKey, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/tenant-override", async (string featureFlagKey, SetTenantOverrideRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetTenantOverrideAsync(featureFlagKey, request.TenantId.Value, request.Enabled, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{featureFlagKey}/rollout-percentage", async (string featureFlagKey, SetRolloutPercentageRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetRolloutPercentageAsync(featureFlagKey, request.RolloutPercentage, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapDelete("/{featureFlagKey}/tenant-override", async (string featureFlagKey, TenantId tenantId, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.RemoveTenantOverrideAsync(featureFlagKey, tenantId.Value, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapGet("/{featureFlagKey}/users", async (string featureFlagKey, string? search, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFeatureFlagUsersAsync(featureFlagKey, search, cancellationToken));
            }
        ).Produces<GetFeatureFlagUsersResponse>();

        group.MapPut("/{featureFlagKey}/user-override", async (string featureFlagKey, SetUserOverrideRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetUserOverrideAsync(featureFlagKey, request.UserId.Value, request.TenantId.Value, request.Enabled, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapDelete("/{featureFlagKey}/user-override", async (string featureFlagKey, UserId userId, TenantId tenantId, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.RemoveUserOverrideAsync(featureFlagKey, userId.Value, tenantId.Value, cancellationToken));
            }
        ).DisableAntiforgery();
    }

    private static async Task<IResult> ProxyResponse(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Results.Content(content, contentType, statusCode: (int)response.StatusCode);
    }
}

public sealed record SetTenantOverrideRequest(TenantId TenantId, bool Enabled);

public sealed record SetUserOverrideRequest(UserId UserId, TenantId TenantId, bool Enabled);

public sealed record SetRolloutPercentageRequest(int RolloutPercentage);
