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
public sealed record ReactivateSubscriptionCommand(SubscriptionPlan Plan, string? SuccessUrl, string? CancelUrl)
    : ICommand, IRequest<Result<ReactivateSubscriptionResponse>>;

[PublicAPI]
public sealed record ReactivateSubscriptionResponse(string? CheckoutUrl);

public sealed class ReactivateSubscriptionValidator : AbstractValidator<ReactivateSubscriptionCommand>
{
    public ReactivateSubscriptionValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Trial).WithMessage("Cannot reactivate to the Trial plan.");
    }
}

public sealed class ReactivateSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
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
            return Result<ReactivateSubscriptionResponse>.NotFound("Subscription not found for current tenant.");
        }

        var stripeClient = stripeClientFactory.GetClient();

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant.State == TenantState.Suspended)
        {
            return await HandleSuspendedReactivation(subscription, command, tenant, stripeClient, cancellationToken);
        }

        if (!subscription.CancelAtPeriodEnd)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Subscription is not cancelled. Nothing to reactivate.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("No active Stripe subscription found.");
        }

        var reactivateSuccess = await stripeClient.ReactivateSubscriptionAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!reactivateSuccess)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to reactivate subscription in Stripe.");
        }

        if (command.Plan > subscription.Plan)
        {
            var upgradeSuccess = await stripeClient.UpgradeSubscriptionAsync(subscription.StripeSubscriptionId, command.Plan, cancellationToken);
            if (!upgradeSuccess)
            {
                return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to upgrade subscription during reactivation.");
            }
        }
        else if (command.Plan < subscription.Plan)
        {
            var downgradeSuccess = await stripeClient.ScheduleDowngradeAsync(subscription.StripeSubscriptionId, command.Plan, cancellationToken);
            if (!downgradeSuccess)
            {
                return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to schedule downgrade during reactivation.");
            }
        }

        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId!, cancellationToken);
        if (syncResult is not null)
        {
            subscription.SyncFromStripe(syncResult.Plan, syncResult.ScheduledPlan, syncResult.StripeSubscriptionId, syncResult.CurrentPeriodEnd, syncResult.CancelAtPeriodEnd, [.. syncResult.PaymentTransactions]);
        }

        subscriptionRepository.Update(subscription);

        events.CollectEvent(new SubscriptionReactivated(subscription.Id, command.Plan));

        return new ReactivateSubscriptionResponse(null);
    }

    private async Task<Result<ReactivateSubscriptionResponse>> HandleSuspendedReactivation(Subscription subscription, ReactivateSubscriptionCommand command, Tenant tenant, IStripeClient stripeClient, CancellationToken cancellationToken)
    {
        if (command.SuccessUrl is null || command.CancelUrl is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Success and cancel URLs are required for suspended subscription reactivation.");
        }

        if (subscription.StripeCustomerId is null)
        {
            var customerId = await stripeClient.CreateCustomerAsync(tenant.Name, executionContext.UserInfo.Email!, cancellationToken);
            if (customerId is null)
            {
                return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to create Stripe customer.");
            }

            subscription.SetStripeCustomerId(customerId);
            subscriptionRepository.Update(subscription);
        }

        var result = await stripeClient.CreateCheckoutSessionAsync(subscription.StripeCustomerId!, command.Plan, command.SuccessUrl, command.CancelUrl, cancellationToken);
        if (result is null)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to create checkout session for reactivation.");
        }

        events.CollectEvent(new SubscriptionReactivated(subscription.Id, command.Plan));

        return new ReactivateSubscriptionResponse(result.Url);
    }
}
