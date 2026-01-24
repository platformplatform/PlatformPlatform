using Microsoft.Extensions.Configuration;
using PlatformPlatform.AccountManagement.Features.Subscriptions.Domain;
using Stripe;
using Stripe.Checkout;
using StripeSubscription = Stripe.Subscription;

namespace PlatformPlatform.AccountManagement.Integrations.Stripe;

public sealed class StripeClient(IConfiguration configuration, ILogger<StripeClient> logger) : IStripeClient
{
    private readonly string? _apiKey = configuration["Stripe:ApiKey"];
    private readonly string? _premiumPriceId = configuration["Stripe:Prices:Premium"];
    private readonly string? _standardPriceId = configuration["Stripe:Prices:Standard"];
    private readonly string? _webhookSecret = configuration["Stripe:WebhookSecret"];

    public async Task<string?> CreateCustomerAsync(string tenantName, string email, CancellationToken cancellationToken)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Name = tenantName,
                Email = email
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, GetRequestOptions(), cancellationToken);

            logger.LogInformation("Created Stripe customer {CustomerId} for tenant {TenantName}", customer.Id, tenantName);

            return customer.Id;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating customer for tenant {TenantName}", tenantName);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating Stripe customer for tenant {TenantName}", tenantName);
            return null;
        }
    }

    public async Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(plan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan {Plan}", plan);
                return null;
            }

            var options = new SessionCreateOptions
            {
                Customer = stripeCustomerId,
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                ]
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, GetRequestOptions(), cancellationToken);

            logger.LogInformation("Created checkout session {SessionId} for customer {CustomerId}", session.Id, stripeCustomerId);

            return new CheckoutSessionResult(session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating checkout session for customer {CustomerId}", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating checkout session for customer {CustomerId}", stripeCustomerId);
            return null;
        }
    }

    public async Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscriptions = await subscriptionService.ListAsync(
                new SubscriptionListOptions { Customer = stripeCustomerId, Limit = 1 },
                GetRequestOptions(), cancellationToken
            );

            var stripeSubscription = subscriptions.Data.FirstOrDefault();
            if (stripeSubscription is null)
            {
                return null;
            }

            var plan = GetPlanFromPriceId(stripeSubscription.Items.Data.FirstOrDefault()?.Price.Id);
            var scheduledPlan = GetScheduledPlan(stripeSubscription);
            var currentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
            var cancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;

            var invoiceService = new InvoiceService();
            var invoices = await invoiceService.ListAsync(
                new InvoiceListOptions { Customer = stripeCustomerId, Limit = 100 },
                GetRequestOptions(), cancellationToken
            );

            var paymentTransactions = invoices.Data.Select(invoice => new PaymentTransaction(
                    PaymentTransactionId.NewId(),
                    invoice.AmountPaid / 100m,
                    invoice.Currency,
                    MapInvoiceStatus(invoice.Status),
                    invoice.PaymentIntent?.Id,
                    invoice.Created,
                    invoice.Status == "uncollectible" ? "Payment failed." : null,
                    invoice.HostedInvoiceUrl
                )
            ).ToArray();

            return new SubscriptionSyncResult(
                plan,
                scheduledPlan,
                stripeSubscription.Id,
                currentPeriodEnd,
                cancelAtPeriodEnd,
                paymentTransactions
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error syncing subscription state for customer {CustomerId}", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout syncing subscription state for customer {CustomerId}", stripeCustomerId);
            return null;
        }
    }

    public async Task<string?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            return session.SubscriptionId;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting checkout session {SessionId}", sessionId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting checkout session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> UpgradeSubscriptionAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(newPlan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan {Plan}", newPlan);
                return false;
            }

            var service = new SubscriptionService();
            var subscription = await service.GetAsync(stripeSubscriptionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            var itemId = subscription.Items.Data.First().Id;

            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    Items =
                    [
                        new SubscriptionItemOptions { Id = itemId, Price = priceId }
                    ],
                    ProrationBehavior = "create_prorations"
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Upgraded subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error upgrading subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout upgrading subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return false;
        }
    }

    public async Task<bool> ScheduleDowngradeAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(newPlan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan {Plan}", newPlan);
                return false;
            }

            var service = new SubscriptionScheduleService();
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            await service.CreateAsync(new SubscriptionScheduleCreateOptions
                {
                    FromSubscription = stripeSubscriptionId,
                    Phases =
                    [
                        new SubscriptionSchedulePhaseOptions
                        {
                            Items = [new SubscriptionSchedulePhaseItemOptions { Price = subscription.Items.Data.First().Price.Id, Quantity = 1 }],
                            StartDate = subscription.CurrentPeriodStart,
                            EndDate = subscription.CurrentPeriodEnd
                        },
                        new SubscriptionSchedulePhaseOptions
                        {
                            Items = [new SubscriptionSchedulePhaseItemOptions { Price = priceId, Quantity = 1 }],
                            StartDate = subscription.CurrentPeriodEnd
                        }
                    ]
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Scheduled downgrade for subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error scheduling downgrade for subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout scheduling downgrade for subscription {SubscriptionId} to {Plan}", stripeSubscriptionId, newPlan);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Scheduled cancellation for subscription {SubscriptionId}", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error cancelling subscription {SubscriptionId}", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout cancelling subscription {SubscriptionId}", stripeSubscriptionId);
            return false;
        }
    }

    public async Task<bool> ReactivateSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = false
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Reactivated subscription {SubscriptionId}", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error reactivating subscription {SubscriptionId}", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout reactivating subscription {SubscriptionId}", stripeSubscriptionId);
            return false;
        }
    }

    public async Task<string?> CreateBillingPortalSessionAsync(string stripeCustomerId, string returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var service = new global::Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(new global::Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = stripeCustomerId,
                    ReturnUrl = returnUrl
                }, GetRequestOptions(), cancellationToken
            );

            return session.Url;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating billing portal session for customer {CustomerId}", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating billing portal session for customer {CustomerId}", stripeCustomerId);
            return null;
        }
    }

    public StripeHealthResult GetHealth()
    {
        return new StripeHealthResult(
            _apiKey is not null,
            _apiKey is not null,
            _webhookSecret is not null,
            _standardPriceId is not null,
            _premiumPriceId is not null
        );
    }

    public StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader)
    {
        try
        {
            if (_webhookSecret is null)
            {
                logger.LogError("Webhook secret is not configured");
                return null;
            }

            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _webhookSecret);
            var customerId = ExtractCustomerId(stripeEvent);

            return new StripeWebhookEventResult(stripeEvent.Id, stripeEvent.Type, customerId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook signature verification failed");
            return null;
        }
    }

    private static string? ExtractCustomerId(Event stripeEvent)
    {
        return stripeEvent.Data.Object switch
        {
            StripeSubscription subscription => subscription.CustomerId,
            Invoice invoice => invoice.CustomerId,
            Session session => session.CustomerId,
            _ => null
        };
    }

    private RequestOptions GetRequestOptions()
    {
        return new RequestOptions { ApiKey = _apiKey };
    }

    private string? GetPriceId(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Standard => _standardPriceId,
            SubscriptionPlan.Premium => _premiumPriceId,
            _ => null
        };
    }

    private SubscriptionPlan GetPlanFromPriceId(string? priceId)
    {
        if (priceId == _standardPriceId) return SubscriptionPlan.Standard;
        if (priceId == _premiumPriceId) return SubscriptionPlan.Premium;
        return SubscriptionPlan.Trial;
    }

    private SubscriptionPlan? GetScheduledPlan(StripeSubscription subscription)
    {
        if (subscription.Schedule?.Phases is null) return null;

        var futurePhase = subscription.Schedule.Phases.LastOrDefault();
        if (futurePhase is null) return null;

        var futurePriceId = futurePhase.Items.FirstOrDefault()?.Price?.ToString();
        if (futurePriceId is null) return null;

        var plan = GetPlanFromPriceId(futurePriceId);
        return plan != GetPlanFromPriceId(subscription.Items.Data.FirstOrDefault()?.Price.Id) ? plan : null;
    }

    private static PaymentTransactionStatus MapInvoiceStatus(string? status)
    {
        return status switch
        {
            "paid" => PaymentTransactionStatus.Succeeded,
            "open" => PaymentTransactionStatus.Pending,
            "uncollectible" => PaymentTransactionStatus.Failed,
            "void" => PaymentTransactionStatus.Refunded,
            _ => PaymentTransactionStatus.Pending
        };
    }
}
