using Microsoft.Extensions.Configuration;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using Stripe;
using Stripe.Checkout;
using DomainPaymentMethod = PlatformPlatform.Account.Features.Subscriptions.Domain.PaymentMethod;
using PaymentMethod = Stripe.PaymentMethod;
using Session = Stripe.Checkout.Session;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using SessionService = Stripe.Checkout.SessionService;
using StripeSubscription = Stripe.Subscription;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class StripeClient(IConfiguration configuration, ILogger<StripeClient> logger) : IStripeClient
{
    private readonly string? _apiKey = configuration["Stripe:ApiKey"];
    private readonly string? _premiumPriceId = configuration["Stripe:Prices:Premium"];
    private readonly string? _standardPriceId = configuration["Stripe:Prices:Standard"];
    private readonly string? _webhookSecret = configuration["Stripe:WebhookSecret"];

    public async Task<string?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Name = tenantName,
                Email = email,
                Metadata = new Dictionary<string, string> { { "tenant_id", tenantId.ToString() } }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, GetRequestOptions(), cancellationToken);

            logger.LogInformation("Created Stripe customer '{CustomerId}' for tenant '{TenantName}'", customer.Id, tenantName);

            return customer.Id;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating customer for tenant '{TenantName}'", tenantName);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating Stripe customer for tenant '{TenantName}'", tenantName);
            return null;
        }
    }

    public async Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string returnUrl, string locale, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(plan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan '{Plan}'", plan);
                return null;
            }

            var options = new SessionCreateOptions
            {
                Customer = stripeCustomerId,
                Mode = "subscription",
                UiMode = "custom",
                Locale = locale,
                BillingAddressCollection = "required",
                CustomerUpdate = new SessionCustomerUpdateOptions { Address = "auto", Name = "auto" },
                ReturnUrl = returnUrl,
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

            logger.LogInformation("Created checkout session '{SessionId}' for customer '{CustomerId}'", session.Id, stripeCustomerId);

            return new CheckoutSessionResult(session.Id, session.ClientSecret);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating checkout session for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating checkout session for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
    }

    public async Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscriptions = await subscriptionService.ListAsync(
                new SubscriptionListOptions { Customer = stripeCustomerId, Limit = 1, Expand = ["data.schedule", "data.default_payment_method", "data.customer.invoice_settings.default_payment_method"] },
                GetRequestOptions(), cancellationToken
            );

            var stripeSubscription = subscriptions.Data.FirstOrDefault();
            if (stripeSubscription is null)
            {
                return null;
            }

            var plan = GetPlanFromPriceId(stripeSubscription.Items.Data.FirstOrDefault()?.Price.Id);
            var scheduledPlan = GetScheduledPlan(stripeSubscription);
            var currentPeriodEnd = stripeSubscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd;
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
                    invoice.Created,
                    invoice.Status == "uncollectible" ? "Payment failed." : null,
                    invoice.InvoicePdf
                )
            ).ToArray();

            DomainPaymentMethod? paymentMethod = null;
            var defaultPaymentMethod = stripeSubscription.DefaultPaymentMethod ?? stripeSubscription.Customer?.InvoiceSettings?.DefaultPaymentMethod;
            if (defaultPaymentMethod is not null)
            {
                if (defaultPaymentMethod.Card is not null)
                {
                    paymentMethod = new DomainPaymentMethod(defaultPaymentMethod.Card.Brand, defaultPaymentMethod.Card.Last4, (int)defaultPaymentMethod.Card.ExpMonth, (int)defaultPaymentMethod.Card.ExpYear);
                }
                else if (defaultPaymentMethod.Link is not null)
                {
                    paymentMethod = new DomainPaymentMethod("link", defaultPaymentMethod.Link.Email?.Substring(defaultPaymentMethod.Link.Email.Length - 4) ?? "****", 0, 0);
                }
            }

            return new SubscriptionSyncResult(
                plan,
                scheduledPlan,
                stripeSubscription.Id,
                currentPeriodEnd,
                cancelAtPeriodEnd,
                paymentTransactions,
                paymentMethod
            );
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error syncing subscription state for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout syncing subscription state for customer '{CustomerId}'", stripeCustomerId);
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
            logger.LogError(ex, "Stripe error getting checkout session '{SessionId}'", sessionId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting checkout session '{SessionId}'", sessionId);
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
                logger.LogError("Price ID not configured for plan '{Plan}'", newPlan);
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
                    ProrationBehavior = "always_invoice"
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Upgraded subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error upgrading subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout upgrading subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
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
                logger.LogError("Price ID not configured for plan '{Plan}'", newPlan);
                return false;
            }

            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            var service = new SubscriptionScheduleService();

            if (subscription.ScheduleId is not null)
            {
                await service.ReleaseAsync(subscription.ScheduleId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            }

            var schedule = await service.CreateAsync(new SubscriptionScheduleCreateOptions
                {
                    FromSubscription = stripeSubscriptionId
                }, GetRequestOptions(), cancellationToken
            );

            var currentPhase = schedule.Phases.First();
            var currentPhaseItems = currentPhase.Items
                .Select(i => new SubscriptionSchedulePhaseItemOptions { Price = i.PriceId, Quantity = i.Quantity }).ToList();

            await service.UpdateAsync(schedule.Id, new SubscriptionScheduleUpdateOptions
                {
                    Phases =
                    [
                        new SubscriptionSchedulePhaseOptions { Items = currentPhaseItems, StartDate = currentPhase.StartDate, EndDate = currentPhase.EndDate },
                        new SubscriptionSchedulePhaseOptions
                        {
                            Items = [new SubscriptionSchedulePhaseItemOptions { Price = priceId, Quantity = 1 }]
                        }
                    ]
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Scheduled downgrade for subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error scheduling downgrade for subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout scheduling downgrade for subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return false;
        }
    }

    public async Task<bool> CancelScheduledDowngradeAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            if (subscription.ScheduleId is null)
            {
                logger.LogWarning("No schedule found for subscription '{SubscriptionId}'", stripeSubscriptionId);
                return true;
            }

            var scheduleService = new SubscriptionScheduleService();
            await scheduleService.ReleaseAsync(subscription.ScheduleId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            logger.LogInformation("Cancelled scheduled downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error cancelling scheduled downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout cancelling scheduled downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            if (subscription.ScheduleId is not null)
            {
                var scheduleService = new SubscriptionScheduleService();
                await scheduleService.ReleaseAsync(subscription.ScheduleId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            }

            await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Scheduled cancellation for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error cancelling subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout cancelling subscription '{SubscriptionId}'", stripeSubscriptionId);
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

            logger.LogInformation("Reactivated subscription '{SubscriptionId}'", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error reactivating subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout reactivating subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
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
            var (customerId, unresolvedChargeId, metadataTenantId) = ExtractCustomerInfo(stripeEvent);

            return new StripeWebhookEventResult(stripeEvent.Id, stripeEvent.Type, customerId, unresolvedChargeId, metadataTenantId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook signature verification failed");
            return null;
        }
    }

    public async Task<string?> GetCustomerIdByChargeAsync(string chargeId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new ChargeService();
            var charge = await service.GetAsync(chargeId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            return charge.CustomerId;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting charge '{ChargeId}'", chargeId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting charge '{ChargeId}'", chargeId);
            return null;
        }
    }

    public async Task<BillingInfo?> GetCustomerBillingInfoAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var customerService = new CustomerService();
            var customer = await customerService.GetAsync(stripeCustomerId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            BillingAddress? address = null;
            var email = customer.Email;

            if (customer.Address is not null)
            {
                address = new BillingAddress(customer.Address.Line1, customer.Address.Line2, customer.Address.PostalCode, customer.Address.City, customer.Address.State, customer.Address.Country);
            }
            else
            {
                var paymentMethodService = new PaymentMethodService();
                var paymentMethods = await paymentMethodService.ListAsync(new PaymentMethodListOptions { Customer = stripeCustomerId, Limit = 1 }, GetRequestOptions(), cancellationToken);
                var billingDetails = paymentMethods.Data.FirstOrDefault()?.BillingDetails;

                if (billingDetails?.Address is { } paymentMethodAddress)
                {
                    address = new BillingAddress(paymentMethodAddress.Line1, paymentMethodAddress.Line2, paymentMethodAddress.PostalCode, paymentMethodAddress.City, paymentMethodAddress.State, paymentMethodAddress.Country);
                    email ??= billingDetails.Email;
                }
            }

            return new BillingInfo(customer.Name, address, email);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting customer billing info for '{CustomerId}'", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting customer billing info for '{CustomerId}'", stripeCustomerId);
            return null;
        }
    }

    public async Task<bool> UpdateCustomerBillingInfoAsync(string stripeCustomerId, BillingInfo billingInfo, CancellationToken cancellationToken)
    {
        try
        {
            var service = new CustomerService();
            await service.UpdateAsync(stripeCustomerId, new CustomerUpdateOptions
                {
                    Name = billingInfo.Name,
                    Email = billingInfo.Email,
                    Address = new AddressOptions
                    {
                        Line1 = billingInfo.Address?.Line1,
                        Line2 = billingInfo.Address?.Line2,
                        City = billingInfo.Address?.City,
                        State = billingInfo.Address?.State,
                        PostalCode = billingInfo.Address?.PostalCode,
                        Country = billingInfo.Address?.Country
                    }
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Updated billing info for customer '{CustomerId}'", stripeCustomerId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error updating billing info for customer '{CustomerId}'", stripeCustomerId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout updating billing info for customer '{CustomerId}'", stripeCustomerId);
            return false;
        }
    }

    public async Task<string?> CreateSetupIntentAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SetupIntentService();
            var setupIntent = await service.CreateAsync(new SetupIntentCreateOptions
                {
                    Customer = stripeCustomerId,
                    AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions { Enabled = true }
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Created SetupIntent for customer '{CustomerId}'", stripeCustomerId);
            return setupIntent.ClientSecret;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error creating SetupIntent for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout creating SetupIntent for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
    }

    public async Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SetupIntentService();
            var setupIntent = await service.GetAsync(setupIntentId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            return setupIntent.PaymentMethodId;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting SetupIntent '{SetupIntentId}'", setupIntentId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting SetupIntent '{SetupIntentId}'", setupIntentId);
            return null;
        }
    }

    public async Task<bool> SetSubscriptionDefaultPaymentMethodAsync(string stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }, GetRequestOptions(), cancellationToken
            );

            logger.LogInformation("Updated default payment method for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error updating payment method for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout updating payment method for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
    }

    private static (string? CustomerId, string? UnresolvedChargeId, long? MetadataTenantId) ExtractCustomerInfo(Event stripeEvent)
    {
        return stripeEvent.Data.Object switch
        {
            Customer customer => (customer.Id, null, ParseMetadataTenantId(customer.Metadata)),
            StripeSubscription subscription => (subscription.CustomerId, null, null),
            Invoice invoice => (invoice.CustomerId, null, null),
            InvoiceItem invoiceItem => (invoiceItem.CustomerId, null, null),
            PaymentIntent paymentIntent => (paymentIntent.CustomerId, null, null),
            Session session => (session.CustomerId, null, null),
            Charge charge => (charge.CustomerId, null, null),
            PaymentMethod paymentMethod => (paymentMethod.CustomerId, null, null),
            Dispute dispute => (dispute.Charge?.CustomerId, dispute.Charge?.CustomerId is null ? dispute.ChargeId : null, null),
            Refund refund => (refund.Charge?.CustomerId, refund.Charge?.CustomerId is null ? refund.ChargeId : null, null),
            _ => (null, null, null)
        };
    }

    private static long? ParseMetadataTenantId(Dictionary<string, string>? metadata)
    {
        if (metadata is not null && metadata.TryGetValue("tenant_id", out var value) && long.TryParse(value, out var tenantId))
        {
            return tenantId;
        }

        return null;
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
        return SubscriptionPlan.Basis;
    }

    private SubscriptionPlan? GetScheduledPlan(StripeSubscription subscription)
    {
        if (subscription.Schedule?.Phases is null) return null;

        var futurePhase = subscription.Schedule.Phases.LastOrDefault();
        if (futurePhase is null) return null;

        var futurePriceId = futurePhase.Items.FirstOrDefault()?.PriceId;
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
