using Account.Features.Subscriptions.Commands;
using Account.Features.Subscriptions.Shared;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;
using Result = SharedKernel.Cqrs.Result;

namespace Account.Api.Endpoints;

public sealed class StripeWebhookEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions/stripe-webhook";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("StripeWebhook").RequireAuthorization().ProducesValidationProblem();

        // Two-phase webhook processing with pessimistic locking requires inline logic beyond 3-line convention
        group.MapPost("/", async Task<ApiResult> (HttpRequest request, IMediator mediator, ProcessPendingStripeEvents processPendingStripeEvents) =>
            {
                var payload = await new StreamReader(request.Body).ReadToEndAsync();
                var signatureHeader = request.Headers["Stripe-Signature"].ToString();
                var acknowledgeResult = await mediator.Send(new AcknowledgeStripeWebhookCommand(payload, signatureHeader));
                if (!acknowledgeResult.IsSuccess) return Result.From(acknowledgeResult);

                var customerId = acknowledgeResult.Value;
                if (customerId is not null)
                {
                    await processPendingStripeEvents.ExecuteAsync(customerId, request.HttpContext.RequestAborted);
                }

                return Result.Success();
            }
        ).AllowAnonymous().DisableAntiforgery();
    }
}
