using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CreateBillingPortalSessionCommand(string ReturnUrl)
    : ICommand, IRequest<Result<CreateBillingPortalSessionResponse>>;

[PublicAPI]
public sealed record CreateBillingPortalSessionResponse(string PortalUrl);

public sealed class CreateBillingPortalSessionValidator : AbstractValidator<CreateBillingPortalSessionCommand>
{
    public CreateBillingPortalSessionValidator()
    {
        RuleFor(x => x.ReturnUrl).NotEmpty().WithMessage("Return URL is required.");
    }
}

public sealed class CreateBillingPortalSessionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<CreateBillingPortalSessionHandler> logger
) : IRequestHandler<CreateBillingPortalSessionCommand, Result<CreateBillingPortalSessionResponse>>
{
    public async Task<Result<CreateBillingPortalSessionResponse>> Handle(CreateBillingPortalSessionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<CreateBillingPortalSessionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for tenant '{TenantId}'", executionContext.TenantId);
            return Result<CreateBillingPortalSessionResponse>.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<CreateBillingPortalSessionResponse>.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var portalUrl = await stripeClient.CreateBillingPortalSessionAsync(subscription.StripeCustomerId, command.ReturnUrl, executionContext.UserInfo.Locale!, cancellationToken);
        if (portalUrl is null)
        {
            return Result<CreateBillingPortalSessionResponse>.BadRequest("Failed to create billing portal session.");
        }

        events.CollectEvent(new BillingPortalSessionCreated(subscription.Id));

        return new CreateBillingPortalSessionResponse(portalUrl);
    }
}
