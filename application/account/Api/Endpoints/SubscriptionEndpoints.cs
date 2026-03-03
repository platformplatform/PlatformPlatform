using Account.Features.Subscriptions.Commands;
using Account.Features.Subscriptions.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class SubscriptionEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Subscriptions").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/pricing-catalog", async Task<ApiResult<PricingCatalogResponse>> ([AsParameters] GetPricingCatalogQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<PricingCatalogResponse>();

        group.MapGet("/checkout-preview", async Task<ApiResult<CheckoutPreviewResponse>> ([AsParameters] GetCheckoutPreviewQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<CheckoutPreviewResponse>();

        group.MapGet("/subscribe-preview", async Task<ApiResult<SubscribePreviewResponse>> ([AsParameters] GetSubscribePreviewQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<SubscribePreviewResponse>();

        group.MapGet("/upgrade-preview", async Task<ApiResult<UpgradePreviewResponse>> ([AsParameters] GetUpgradePreviewQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UpgradePreviewResponse>();

        group.MapPost("/start-checkout", async Task<ApiResult<StartSubscriptionCheckoutResponse>> (StartSubscriptionCheckoutCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartSubscriptionCheckoutResponse>();

        group.MapPost("/upgrade", async Task<ApiResult<UpgradeSubscriptionResponse>> (UpgradeSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<UpgradeSubscriptionResponse>();

        group.MapPost("/schedule-downgrade", async Task<ApiResult> (ScheduleDowngradeCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/cancel-downgrade", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new CancelScheduledDowngradeCommand())
        );

        group.MapPost("/cancel", async Task<ApiResult> (CancelSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/reactivate", async Task<ApiResult<ReactivateSubscriptionResponse>> (IMediator mediator)
            => await mediator.Send(new ReactivateSubscriptionCommand())
        ).Produces<ReactivateSubscriptionResponse>();

        group.MapPost("/process-pending-events", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new ProcessPendingEventsCommand())
        );
    }
}
