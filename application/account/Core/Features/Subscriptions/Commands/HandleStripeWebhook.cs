using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record HandleStripeWebhookCommand(string Payload, string SignatureHeader) : ICommand, IRequest<Result>;

public sealed class HandleStripeWebhookHandler(
    ISubscriptionRepository subscriptionRepository,
    IStripeEventRepository stripeEventRepository,
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    StripeClientFactory stripeClientFactory,
    IEmailClient emailClient,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<HandleStripeWebhookCommand, Result>
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(72);
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromHours(24);

    public async Task<Result> Handle(HandleStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var webhookEvent = stripeClient.VerifyWebhookSignature(command.Payload, command.SignatureHeader);
        if (webhookEvent is null)
        {
            return Result.BadRequest("Invalid webhook signature.");
        }

        if (await stripeEventRepository.ExistsAsync(webhookEvent.EventId, cancellationToken))
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();

        var customerId = webhookEvent.CustomerId;
        if (customerId is null && webhookEvent.UnresolvedChargeId is not null)
        {
            customerId = await stripeClient.GetCustomerIdByChargeAsync(webhookEvent.UnresolvedChargeId, cancellationToken);
        }

        if (customerId is null)
        {
            await RecordEvent(webhookEvent, now, null, null, null, command.Payload, cancellationToken);
            return Result.Success();
        }

        var subscription = await subscriptionRepository.GetByStripeCustomerIdUnfilteredAsync(customerId, cancellationToken);
        if (subscription is null)
        {
            await RecordEvent(webhookEvent, now, customerId, null, webhookEvent.MetadataTenantId, command.Payload, cancellationToken);
            return Result.Success();
        }

        var stripeSubscriptionId = subscription.StripeSubscriptionId;

        var syncResult = await stripeClient.SyncSubscriptionStateAsync(customerId, cancellationToken);
        if (syncResult is not null)
        {
            subscription.SyncFromStripe(
                syncResult.Plan,
                syncResult.ScheduledPlan,
                syncResult.StripeSubscriptionId,
                syncResult.CurrentPeriodEnd,
                syncResult.CancelAtPeriodEnd,
                [.. syncResult.PaymentTransactions],
                syncResult.PaymentMethod
            );
        }

        var billingInfo = await stripeClient.GetCustomerBillingInfoAsync(customerId, cancellationToken);
        subscription.SetBillingInfo(billingInfo);

        if (webhookEvent.EventType == "invoice.payment_succeeded")
        {
            await HandlePaymentSucceeded(subscription, cancellationToken);
        }
        else if (webhookEvent.EventType == "invoice.payment_failed")
        {
            await HandlePaymentFailed(subscription, now, cancellationToken);
        }
        else if (webhookEvent.EventType == "charge.dispute.created")
        {
            await HandleDisputeCreated(subscription, now, cancellationToken);
        }
        else if (webhookEvent.EventType == "charge.dispute.closed")
        {
            HandleDisputeClosed(subscription);
        }
        else if (webhookEvent.EventType == "charge.refunded")
        {
            HandleRefund(subscription, now);
        }
        else if (webhookEvent.EventType == "checkout.session.completed")
        {
            events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan));
        }
        else if (webhookEvent.EventType == "customer.subscription.deleted")
        {
            if (syncResult is null)
            {
                subscription.ResetToFreePlan();
            }

            await HandleSubscriptionDeleted(subscription, cancellationToken);
        }

        subscriptionRepository.Update(subscription);

        await RecordEvent(webhookEvent, now, customerId, stripeSubscriptionId, subscription.TenantId.Value, command.Payload, cancellationToken);
        events.CollectEvent(new WebhookProcessed(subscription.Id, webhookEvent.EventType));

        return Result.Success();
    }

    private async Task HandlePaymentSucceeded(Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.FirstPaymentFailedAt is null) return;

        subscription.ClearPaymentFailure();

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken);
        if (tenant is not null && tenant.State == TenantState.PastDue)
        {
            tenant.SetState(TenantState.Active);
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan));
    }

    private async Task HandlePaymentFailed(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken);
        var owner = await userRepository.GetOwnerByTenantIdUnfilteredAsync(subscription.TenantId, cancellationToken);

        if (subscription.FirstPaymentFailedAt is null)
        {
            subscription.SetPaymentFailed(now);
            subscription.SetLastNotificationSentAt(now);

            if (tenant is not null && tenant.State != TenantState.Suspended)
            {
                tenant.SetState(TenantState.PastDue);
                tenantRepository.Update(tenant);
            }

            if (owner is not null)
            {
                await SendPaymentFailedEmail(owner.Email, cancellationToken);
            }

            events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan));
        }
        else
        {
            var timeSinceFirstFailure = now - subscription.FirstPaymentFailedAt.Value;

            if (timeSinceFirstFailure >= GracePeriod)
            {
                if (tenant is not null && tenant.State != TenantState.Suspended)
                {
                    tenant.SetState(TenantState.Suspended);
                    tenantRepository.Update(tenant);
                }

                if (owner is not null)
                {
                    await SendSubscriptionSuspendedEmail(owner.Email, cancellationToken);
                }

                events.CollectEvent(new SubscriptionSuspended(subscription.Id, subscription.Plan));
            }
            else
            {
                var shouldSendReminder = subscription.LastNotificationSentAt is null ||
                                         now - subscription.LastNotificationSentAt.Value >= NotificationCooldown;

                if (shouldSendReminder && owner is not null)
                {
                    var hoursRemaining = (GracePeriod - timeSinceFirstFailure).TotalHours;
                    var daysRemaining = Math.Max(1, (int)Math.Ceiling(hoursRemaining / 24));
                    await SendGracePeriodReminderEmail(owner.Email, daysRemaining, cancellationToken);
                    subscription.SetLastNotificationSentAt(now);
                }
            }
        }
    }

    private async Task HandleSubscriptionDeleted(Subscription subscription, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken);
        if (tenant is not null && tenant.State != TenantState.Suspended)
        {
            tenant.SetState(TenantState.Suspended);
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new SubscriptionSuspended(subscription.Id, subscription.Plan));
    }

    private async Task SendPaymentFailedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Payment failed - action required";
        const string htmlContent = """
                                   <h2>Your payment has failed</h2>
                                   <p>We were unable to process your subscription payment. Your subscription is now past due.</p>
                                   <p>Please update your payment method to avoid service interruption. You have 3 days to resolve this before your subscription is suspended.</p>
                                   <p>You can update your payment method from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task SendGracePeriodReminderEmail(string recipientEmail, int daysRemaining, CancellationToken cancellationToken)
    {
        const string subject = "Payment reminder - subscription at risk";
        var htmlContent = $"""
                           <h2>Payment still outstanding</h2>
                           <p>Your subscription payment is still failing. You have approximately {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining before your subscription is suspended.</p>
                           <p>Please update your payment method as soon as possible to avoid losing access.</p>
                           <p>You can update your payment method from your subscription settings.</p>
                           """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task SendSubscriptionSuspendedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Subscription suspended";
        const string htmlContent = """
                                   <h2>Your subscription has been suspended</h2>
                                   <p>Due to continued payment failure, your subscription has been suspended. All users in your organization will have limited access until the subscription is reactivated.</p>
                                   <p>To restore access, please update your payment method and reactivate your subscription from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task HandleDisputeCreated(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        subscription.SetDisputed(now);

        var owner = await userRepository.GetOwnerByTenantIdUnfilteredAsync(subscription.TenantId, cancellationToken);
        if (owner is not null)
        {
            await SendDisputeCreatedEmail(owner.Email, cancellationToken);
        }

        events.CollectEvent(new PaymentDisputed(subscription.Id, subscription.Plan));
    }

    private void HandleDisputeClosed(Subscription subscription)
    {
        subscription.ClearDispute();
        events.CollectEvent(new DisputeResolved(subscription.Id, subscription.Plan));
    }

    private void HandleRefund(Subscription subscription, DateTimeOffset now)
    {
        subscription.SetRefunded(now);
        events.CollectEvent(new PaymentRefunded(subscription.Id, subscription.Plan));
    }

    private async Task SendDisputeCreatedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Payment dispute - immediate action required";
        const string htmlContent = """
                                   <h2>A payment dispute has been filed</h2>
                                   <p>A payment dispute has been filed for your subscription.</p>
                                   <p>Please review the dispute in your payment settings or contact support for assistance.</p>
                                   <p>You can manage your subscription from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task RecordEvent(StripeWebhookEventResult webhookEvent, DateTimeOffset now, string? stripeCustomerId, string? stripeSubscriptionId, long? tenantId, string? payload, CancellationToken cancellationToken)
    {
        var record = StripeEvent.Create(webhookEvent.EventId, webhookEvent.EventType, now, stripeCustomerId, stripeSubscriptionId, tenantId, payload);
        await stripeEventRepository.AddAsync(record, cancellationToken);
    }
}
