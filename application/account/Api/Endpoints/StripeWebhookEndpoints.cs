using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.Account.Api.Endpoints;

public sealed class StripeWebhookEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions/stripe-webhook";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("StripeWebhook").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/", async Task<ApiResult> (HttpRequest request, IMediator mediator) =>
            {
                var payload = await new StreamReader(request.Body).ReadToEndAsync();
                var signatureHeader = request.Headers["Stripe-Signature"].ToString();
                return await mediator.Send(new HandleStripeWebhookCommand(payload, signatureHeader));
            }
        ).AllowAnonymous().DisableAntiforgery();
    }
}
