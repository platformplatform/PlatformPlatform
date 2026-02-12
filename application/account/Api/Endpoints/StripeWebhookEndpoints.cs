using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.Account.Api.Endpoints;

public sealed class StripeWebhookEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/subscriptions/stripe-webhook";
    private const int MaxRetries = 3;

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("StripeWebhook").RequireAuthorization().ProducesValidationProblem();

        // Stripe fires multiple events simultaneously (e.g., checkout.session.completed,
        // customer.subscription.created, invoice.payment_succeeded), causing concurrent updates
        // to the same subscription row. Each retry uses a fresh DI scope so EF Core gets a new
        // DbContext with up-to-date concurrency tokens.
        group.MapPost("/", async Task<ApiResult> (HttpRequest request, IServiceScopeFactory scopeFactory) =>
            {
                var payload = await new StreamReader(request.Body).ReadToEndAsync();
                var signatureHeader = request.Headers["Stripe-Signature"].ToString();
                var command = new HandleStripeWebhookCommand(payload, signatureHeader);

                for (var attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    if (attempt > 1) await Task.Delay(Random.Shared.Next(100, 500) * attempt);

                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    try
                    {
                        return await mediator.Send(command);
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
                    {
                    }
                }

                throw new UnreachableException();
            }
        ).AllowAnonymous().DisableAntiforgery();
    }
}
