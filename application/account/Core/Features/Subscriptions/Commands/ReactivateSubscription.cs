using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Shared;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ReactivateSubscriptionCommand(SubscriptionPlan Plan, string? ReturnUrl)
    : ICommand, IRequest<Result<ReactivateSubscriptionResponse>>;

[PublicAPI]
public sealed record ReactivateSubscriptionResponse(string? ClientSecret, string? PublishableKey);

public sealed class ReactivateSubscriptionValidator : AbstractValidator<ReactivateSubscriptionCommand>
{
    public ReactivateSubscriptionValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot reactivate to the Basis plan.");
    }
}

public sealed class ReactivateSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<ReactivateSubscriptionHandler> logger
) : IRequestHandler<ReactivateSubscriptionCommand, Result<ReactivateSubscriptionResponse>>
{
    public async Task<Result<ReactivateSubscriptionResponse>> Handle(ReactivateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<ReactivateSubscriptionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for tenant '{TenantId}'", executionContext.TenantId);
            return Result<ReactivateSubscriptionResponse>.NotFound("Subscription not found for current tenant.");
        }

        var stripeClient = stripeClientFactory.GetClient();

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant.State == TenantState.Suspended)
        {
            return await HandleSuspendedReactivation(subscription, command, stripeClient, cancellationToken);
        }

        if (!subscription.CancelAtPeriodEnd)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Subscription is not cancelled. Nothing to reactivate.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<ReactivateSubscriptionResponse>.BadRequest("No active Stripe subscription found.");
        }

        var reactivateSuccess = await stripeClient.ReactivateSubscriptionAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!reactivateSuccess)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to reactivate subscription in Stripe.");
        }

        if (command.Plan.IsUpgradeFrom(subscription.Plan))
        {
            var upgradeSuccess = await stripeClient.UpgradeSubscriptionAsync(subscription.StripeSubscriptionId, command.Plan, cancellationToken);
            if (!upgradeSuccess)
            {
                return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to upgrade subscription during reactivation.");
            }
        }
        else if (command.Plan.IsDowngradeFrom(subscription.Plan))
        {
            var downgradeSuccess = await stripeClient.ScheduleDowngradeAsync(subscription.StripeSubscriptionId, command.Plan, cancellationToken);
            if (!downgradeSuccess)
            {
                return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to schedule downgrade during reactivation.");
            }
        }

        events.CollectEvent(new SubscriptionReactivated(subscription.Id, command.Plan));

        return new ReactivateSubscriptionResponse(null, null);
    }

    private async Task<Result<ReactivateSubscriptionResponse>> HandleSuspendedReactivation(Subscription subscription, ReactivateSubscriptionCommand command, IStripeClient stripeClient, CancellationToken cancellationToken)
    {
        if (command.ReturnUrl is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Return URL is required for suspended subscription reactivation.");
        }

        var publishableKey = stripeClientFactory.GetPublishableKey();
        if (publishableKey is null)
        {
            logger.LogWarning("Stripe publishable key is not configured");
            return Result<ReactivateSubscriptionResponse>.BadRequest("Stripe is not configured for checkout.");
        }

        if (subscription.StripeCustomerId is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Billing information must be saved before checkout.");
        }

        var result = await stripeClient.CreateCheckoutSessionAsync(subscription.StripeCustomerId!, command.Plan, command.ReturnUrl, executionContext.UserInfo.Locale, cancellationToken);
        if (result is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to create checkout session for reactivation.");
        }

        events.CollectEvent(new SubscriptionReactivated(subscription.Id, command.Plan));

        return new ReactivateSubscriptionResponse(result.ClientSecret, publishableKey);
    }
}
