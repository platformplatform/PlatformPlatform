using Account.Features.Tenants.BackOffice.Commands;
using Account.Features.Tenants.BackOffice.Queries;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Domain;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class TenantsEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeTenants")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<TenantsResponse>> ([AsParameters] GetTenantsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<TenantsResponse>();

        group.MapGet("/{id}", async Task<ApiResult<TenantDetailResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new GetTenantDetailQuery(id))
        ).Produces<TenantDetailResponse>();

        group.MapGet("/{id}/user-counts", async Task<ApiResult<TenantUserCountsResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new GetTenantUserCountsQuery(id))
        ).Produces<TenantUserCountsResponse>();

        group.MapGet("/{id}/users", async Task<ApiResult<TenantUsersResponse>> (TenantId id, [AsParameters] GetTenantUsersQuery query, IMediator mediator)
            => await mediator.Send(query with { Id = id })
        ).Produces<TenantUsersResponse>();

        group.MapGet("/{id}/activity", async Task<ApiResult<TenantActivityResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new GetTenantActivityQuery(id))
        ).Produces<TenantActivityResponse>();

        group.MapGet("/{id}/payment-history", async Task<ApiResult<TenantPaymentHistoryResponse>> (TenantId id, [AsParameters] GetTenantPaymentHistoryQuery query, IMediator mediator)
            => await mediator.Send(query with { Id = id })
        ).Produces<TenantPaymentHistoryResponse>();

        group.MapPost("/{id}/reconcile-with-stripe", async Task<ApiResult<ReconcileTenantWithStripeResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new ReconcileTenantWithStripeCommand { TenantId = id })
        ).Produces<ReconcileTenantWithStripeResponse>().RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapPost("/{id}/replay-archived-stripe-events", async Task<ApiResult<ReplayArchivedTenantStripeEventsResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new ReplayArchivedTenantStripeEventsCommand { TenantId = id })
        ).Produces<ReplayArchivedTenantStripeEventsResponse>().RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);

        group.MapPost("/{id}/drift/acknowledge", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new AcknowledgeBillingDriftCommand { TenantId = id })
        ).RequireAuthorization(BackOfficeIdentityDefaults.AdminPolicyName);
    }
}
