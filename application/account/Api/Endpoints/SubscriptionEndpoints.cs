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

        group.MapGet("/upgrade-preview", async Task<ApiResult<UpgradePreviewResponse>> ([AsParameters] GetUpgradePreviewQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UpgradePreviewResponse>();

        group.MapGet("/checkout-preview", async Task<ApiResult<CheckoutPreviewResponse>> ([AsParameters] GetCheckoutPreviewQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<CheckoutPreviewResponse>();

        group.MapGet("/pricing-catalog", async Task<ApiResult<PricingCatalogResponse>> ([AsParameters] GetPricingCatalogQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<PricingCatalogResponse>();

        group.MapPost("/checkout", async Task<ApiResult<CreateCheckoutSessionResponse>> (CreateCheckoutSessionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<CreateCheckoutSessionResponse>();

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

        group.MapPost("/reactivate", async Task<ApiResult<ReactivateSubscriptionResponse>> (ReactivateSubscriptionCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<ReactivateSubscriptionResponse>();

        group.MapPost("/payment-method-setup", async Task<ApiResult<CreatePaymentMethodSetupResponse>> (IMediator mediator)
            => await mediator.Send(new CreatePaymentMethodSetupCommand())
        ).Produces<CreatePaymentMethodSetupResponse>();

        group.MapPost("/confirm-payment-method", async Task<ApiResult> (ConfirmPaymentMethodSetupCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPut("/billing-info", async Task<ApiResult> (UpdateBillingInfoCommand command, IMediator mediator)
            => await mediator.Send(command)
        );
    }
}
