using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Subscriptions.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CreateCheckoutSessionCommand(SubscriptionPlan Plan, string SuccessUrl, string CancelUrl)
    : ICommand, IRequest<Result<CreateCheckoutSessionResponse>>;

[PublicAPI]
public sealed record CreateCheckoutSessionResponse(string CheckoutUrl);

public sealed class CreateCheckoutSessionValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionValidator()
    {
        RuleFor(x => x.SuccessUrl).NotEmpty().WithMessage("Success URL is required.");
        RuleFor(x => x.CancelUrl).NotEmpty().WithMessage("Cancel URL is required.");
    }
}

public sealed class CreateCheckoutSessionHandler(
    ISubscriptionRepository subscriptionRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<CreateCheckoutSessionCommand, Result<CreateCheckoutSessionResponse>>
{
    public async Task<Result<CreateCheckoutSessionResponse>> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<CreateCheckoutSessionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        if (executionContext.UserInfo.Email is null)
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("User email is required to create a checkout session.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            return Result<CreateCheckoutSessionResponse>.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.HasActiveStripeSubscription())
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("An active subscription already exists. Cannot create a new checkout session.");
        }

        if (command.Plan == SubscriptionPlan.Trial)
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("Cannot create a checkout session for the Trial plan.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            var customerId = await stripeClient.CreateCustomerAsync(tenant.Name, executionContext.UserInfo.Email, cancellationToken);
            if (customerId is null)
            {
                return Result<CreateCheckoutSessionResponse>.BadRequest("Failed to create Stripe customer.");
            }

            subscription.SetStripeCustomerId(customerId);
            subscriptionRepository.Update(subscription);
        }

        var result = await stripeClient.CreateCheckoutSessionAsync(subscription.StripeCustomerId!, command.Plan, command.SuccessUrl, command.CancelUrl, cancellationToken);
        if (result is null)
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("Failed to create checkout session.");
        }

        events.CollectEvent(new CheckoutSessionCreated(subscription.Id, command.Plan));

        return new CreateCheckoutSessionResponse(result.Url);
    }
}
