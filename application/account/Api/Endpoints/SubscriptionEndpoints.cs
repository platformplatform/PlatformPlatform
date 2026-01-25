using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.Account.Api.Endpoints;

public sealed class SubscriptionEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Subscriptions").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/current", async Task<ApiResult<SubscriptionResponse>> ([AsParameters] GetCurrentSubscriptionQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<SubscriptionResponse>();

        group.MapGet("/payment-history", async Task<ApiResult<PaymentHistoryResponse>> ([AsParameters] GetPaymentHistoryQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<PaymentHistoryResponse>();

        group.MapGet("/stripe-health", async Task<ApiResult<StripeHealthResponse>> ([AsParameters] GetStripeHealthQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<StripeHealthResponse>();

        group.MapPost("/checkout", async Task<ApiResult<CreateCheckoutSessionResponse>> (CreateCheckoutSessionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<CreateCheckoutSessionResponse>();

        group.MapPost("/checkout-success", async Task<ApiResult> (CheckoutSuccessCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/upgrade", async Task<ApiResult> (UpgradeSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/schedule-downgrade", async Task<ApiResult> (ScheduleDowngradeCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/cancel", async Task<ApiResult> (CancelSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/reactivate", async Task<ApiResult<ReactivateSubscriptionResponse>> (ReactivateSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<ReactivateSubscriptionResponse>();

        group.MapPost("/billing-portal", async Task<ApiResult<CreateBillingPortalSessionResponse>> (CreateBillingPortalSessionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<CreateBillingPortalSessionResponse>();
    }
}
