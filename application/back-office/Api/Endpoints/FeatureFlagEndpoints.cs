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

        group.MapGet("/{flagKey}/tenants", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFlagTenantsAsync(flagKey, cancellationToken));
            }
        ).Produces<GetFlagTenantsResponse>();

        group.MapPut("/{flagKey}/activate", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.ActivateFlagAsync(flagKey, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{flagKey}/deactivate", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.DeactivateFlagAsync(flagKey, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{flagKey}/tenant-override", async (string flagKey, SetTenantOverrideRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetTenantOverrideAsync(flagKey, request.TenantId.Value, request.Enabled, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapPut("/{flagKey}/rollout-percentage", async (string flagKey, SetRolloutPercentageRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetRolloutPercentageAsync(flagKey, request.RolloutPercentage, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapDelete("/{flagKey}/tenant-override", async (string flagKey, TenantId tenantId, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.RemoveTenantOverrideAsync(flagKey, tenantId.Value, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapGet("/{flagKey}/users", async (string flagKey, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.GetFlagUsersAsync(flagKey, cancellationToken));
            }
        ).Produces<GetFlagUsersResponse>();

        group.MapPut("/{flagKey}/user-override", async (string flagKey, SetUserOverrideRequest request, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.SetUserOverrideAsync(flagKey, request.UserId.Value, request.TenantId.Value, request.Enabled, cancellationToken));
            }
        ).DisableAntiforgery();

        group.MapDelete("/{flagKey}/user-override", async (string flagKey, UserId userId, TenantId tenantId, AccountApiClient accountApiClient, IExecutionContext executionContext, CancellationToken cancellationToken) =>
            {
                if (!executionContext.UserInfo.IsInternalUser) return Results.Forbid();
                return await ProxyResponse(accountApiClient.RemoveUserOverrideAsync(flagKey, userId.Value, tenantId.Value, cancellationToken));
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
