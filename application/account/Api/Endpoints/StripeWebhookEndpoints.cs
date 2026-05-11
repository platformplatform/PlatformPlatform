using Account.Features.Subscriptions.Commands;
using Account.Features.Subscriptions.Shared;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;
using SharedKernel.OpenApi;
using Result = SharedKernel.Cqrs.Result;

namespace Account.Api.Endpoints;

public sealed class StripeWebhookEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions/stripe-webhook";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("StripeWebhook").WithGroupName(OpenApiDocumentNames.Account).RequireAuthorization().ProducesValidationProblem();

        // Two-phase webhook processing with pessimistic locking requires inline logic beyond 3-line convention
        group.MapPost("/", async Task<ApiResult> (HttpRequest request, IMediator mediator, ProcessPendingStripeEvents processPendingStripeEvents) =>
            {
                var payload = await new StreamReader(request.Body).ReadToEndAsync();
                if (!request.Headers.TryGetValue("Stripe-Signature", out var signatureHeaderValues) || signatureHeaderValues.Count != 1)
                {
                    return Result.BadRequest("Stripe-Signature header missing or duplicated.");
                }

                var signatureHeader = signatureHeaderValues[0]!;
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
