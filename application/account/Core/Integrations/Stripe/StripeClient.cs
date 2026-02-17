using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using Stripe;
using Stripe.Checkout;
using DomainPaymentMethod = PlatformPlatform.Account.Features.Subscriptions.Domain.PaymentMethod;
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

    public async Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Name = tenantName,
                BusinessName = tenantName,
                Email = email,
                Metadata = new Dictionary<string, string> { { "TenantId", tenantId.ToString() } }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, GetRequestOptions(), cancellationToken);

            logger.LogInformation("Created Stripe customer '{CustomerId}' for tenant '{TenantName}'", customer.Id, tenantName);

            return StripeCustomerId.NewId(customer.Id);
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

    public async Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string returnUrl, string? locale, CancellationToken cancellationToken)
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
                Customer = stripeCustomerId.Value,
                Mode = "subscription",
                UiMode = "custom",
                Locale = string.IsNullOrEmpty(locale) ? "auto" : locale[..2],
                BillingAddressCollection = "required",
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
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

    public async Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscriptions = await subscriptionService.ListAsync(
                new SubscriptionListOptions { Customer = stripeCustomerId.Value, Limit = 1, Expand = ["data.schedule", "data.default_payment_method", "data.customer.invoice_settings.default_payment_method"] },
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
                new InvoiceListOptions { Customer = stripeCustomerId.Value, Limit = 100 },
                GetRequestOptions(), cancellationToken
            );

            var creditNoteService = new CreditNoteService();
            var creditNotes = await creditNoteService.ListAsync(
                new CreditNoteListOptions { Customer = stripeCustomerId.Value, Limit = 100 },
                GetRequestOptions(), cancellationToken
            );
            var creditNotesByInvoiceId = creditNotes.Data.GroupBy(cn => cn.InvoiceId).ToDictionary(g => g.Key, g => g.First().Pdf);

            var paymentTransactions = invoices.Data.Select(invoice => new PaymentTransaction(
                    PaymentTransactionId.NewId(),
                    invoice.AmountPaid / 100m,
                    invoice.Currency,
                    MapInvoiceStatus(invoice.Status, invoice.AmountPaid, invoice.PostPaymentCreditNotesAmount),
                    invoice.Created,
                    invoice.Status == "uncollectible" ? "Payment failed." : null,
                    invoice.InvoicePdf,
                    creditNotesByInvoiceId.GetValueOrDefault(invoice.Id)
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
                StripeSubscriptionId.NewId(stripeSubscription.Id),
                currentPeriodEnd,
                cancelAtPeriodEnd,
                paymentTransactions,
                paymentMethod,
                stripeSubscription.Status
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

    public async Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            return session.SubscriptionId is not null ? StripeSubscriptionId.NewId(session.SubscriptionId) : null;
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

    public async Task<UpgradeSubscriptionResult?> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(newPlan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan '{Plan}'", newPlan);
                return null;
            }

            var service = new SubscriptionService();
            var subscription = await service.GetAsync(stripeSubscriptionId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            var itemId = subscription.Items.Data.First().Id;

            var updatedSubscription = await service.UpdateAsync(stripeSubscriptionId.Value, new SubscriptionUpdateOptions
                {
                    Items =
                    [
                        new SubscriptionItemOptions { Id = itemId, Price = priceId }
                    ],
                    ProrationBehavior = "always_invoice",
                    AutomaticTax = new SubscriptionAutomaticTaxOptions { Enabled = true }
                }, GetRequestOptions(), cancellationToken
            );

            string? clientSecret = null;
            if (updatedSubscription.LatestInvoiceId is not null)
            {
                var invoiceService = new InvoiceService();
                var invoice = await invoiceService.GetAsync(updatedSubscription.LatestInvoiceId, new InvoiceGetOptions
                    {
                        Expand = ["payments.data.payment.payment_intent"]
                    }, GetRequestOptions(), cancellationToken
                );

                clientSecret = invoice.Payments?.Data
                    .FirstOrDefault(p => p.Payment?.PaymentIntent?.Status == "requires_action")
                    ?.Payment?.PaymentIntent?.ClientSecret;
            }

            logger.LogInformation("Upgraded subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return new UpgradeSubscriptionResult(clientSecret);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error upgrading subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout upgrading subscription '{SubscriptionId}' to '{Plan}'", stripeSubscriptionId, newPlan);
            return null;
        }
    }

    public async Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
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
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            var service = new SubscriptionScheduleService();

            if (subscription.ScheduleId is not null)
            {
                await service.ReleaseAsync(subscription.ScheduleId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            }

            var schedule = await service.CreateAsync(new SubscriptionScheduleCreateOptions
                {
                    FromSubscription = stripeSubscriptionId.Value
                }, GetRequestOptions(), cancellationToken
            );

            var currentPhase = schedule.Phases.First();
            var currentPhaseItems = currentPhase.Items
                .Select(i => new SubscriptionSchedulePhaseItemOptions { Price = i.PriceId, Quantity = i.Quantity }).ToList();

            await service.UpdateAsync(schedule.Id, new SubscriptionScheduleUpdateOptions
                {
                    Phases =
                    [
                        new SubscriptionSchedulePhaseOptions { Items = currentPhaseItems, StartDate = currentPhase.StartDate, EndDate = currentPhase.EndDate, AutomaticTax = new SubscriptionSchedulePhaseAutomaticTaxOptions { Enabled = true } },
                        new SubscriptionSchedulePhaseOptions
                        {
                            Items = [new SubscriptionSchedulePhaseItemOptions { Price = priceId, Quantity = 1 }],
                            AutomaticTax = new SubscriptionSchedulePhaseAutomaticTaxOptions { Enabled = true }
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

    public async Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

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

    public async Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationReason reason, string? feedback, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            if (subscription.ScheduleId is not null)
            {
                var scheduleService = new SubscriptionScheduleService();
                await scheduleService.ReleaseAsync(subscription.ScheduleId, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            }

            await subscriptionService.UpdateAsync(stripeSubscriptionId.Value, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true,
                    CancellationDetails = new SubscriptionCancellationDetailsOptions
                    {
                        Feedback = MapCancellationFeedback(reason),
                        Comment = feedback
                    }
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

    public async Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId.Value, new SubscriptionUpdateOptions
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
            var customerId = ExtractCustomerId(payload);

            return new StripeWebhookEventResult(stripeEvent.Id, stripeEvent.Type, customerId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook signature verification failed");
            return null;
        }
    }

    public async Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var customerService = new CustomerService();
            var customer = await customerService.GetAsync(stripeCustomerId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);

            if (customer.Deleted == true)
            {
                return new CustomerBillingResult(null, true);
            }

            BillingAddress? address = null;
            var email = customer.Email;

            if (customer.Address is not null)
            {
                address = new BillingAddress(customer.Address.Line1, customer.Address.Line2, customer.Address.PostalCode, customer.Address.City, customer.Address.State, customer.Address.Country);
            }
            else
            {
                var paymentMethodService = new PaymentMethodService();
                var paymentMethods = await paymentMethodService.ListAsync(new PaymentMethodListOptions { Customer = stripeCustomerId.Value, Limit = 1 }, GetRequestOptions(), cancellationToken);
                var billingDetails = paymentMethods.Data.FirstOrDefault()?.BillingDetails;

                if (billingDetails?.Address is { } paymentMethodAddress)
                {
                    address = new BillingAddress(paymentMethodAddress.Line1, paymentMethodAddress.Line2, paymentMethodAddress.PostalCode, paymentMethodAddress.City, paymentMethodAddress.State, paymentMethodAddress.Country);
                    email ??= billingDetails.Email;
                }
            }

            var taxIdService = new CustomerTaxIdService();
            var taxIds = await taxIdService.ListAsync(stripeCustomerId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            var taxId = taxIds.Data.FirstOrDefault()?.Value;

            return new CustomerBillingResult(new BillingInfo(customer.Name, address, email, taxId), false);
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

    public async Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string? locale, CancellationToken cancellationToken)
    {
        try
        {
            var service = new CustomerService();
            await service.UpdateAsync(stripeCustomerId.Value, new CustomerUpdateOptions
                {
                    Name = billingInfo.Name,
                    BusinessName = billingInfo.Name,
                    Email = billingInfo.Email,
                    Address = new AddressOptions
                    {
                        Line1 = billingInfo.Address?.Line1,
                        Line2 = billingInfo.Address?.Line2,
                        City = billingInfo.Address?.City,
                        State = billingInfo.Address?.State,
                        PostalCode = billingInfo.Address?.PostalCode,
                        Country = billingInfo.Address?.Country
                    },
                    PreferredLocales = locale == "da-DK" ? ["da", "en"] : locale is not null ? [locale[..2]] : ["en"]
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

    public async Task<bool> SyncCustomerTaxIdAsync(StripeCustomerId stripeCustomerId, string? taxId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new CustomerTaxIdService();
            var existingTaxIds = await service.ListAsync(stripeCustomerId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            foreach (var existing in existingTaxIds.Data)
            {
                await service.DeleteAsync(stripeCustomerId.Value, existing.Id, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(taxId))
            {
                var taxIdType = InferTaxIdType(taxId);
                await service.CreateAsync(stripeCustomerId.Value, new CustomerTaxIdCreateOptions
                    {
                        Type = taxIdType,
                        Value = taxId
                    }, GetRequestOptions(), cancellationToken
                );
            }

            logger.LogInformation("Synced tax ID for customer '{CustomerId}'", stripeCustomerId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error syncing tax ID for customer '{CustomerId}'", stripeCustomerId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout syncing tax ID for customer '{CustomerId}'", stripeCustomerId);
            return false;
        }
    }

    public async Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SetupIntentService();
            var setupIntent = await service.CreateAsync(new SetupIntentCreateOptions
                {
                    Customer = stripeCustomerId.Value,
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

    public async Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId.Value, new SubscriptionUpdateOptions
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

    public async Task<bool?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        try
        {
            var invoiceService = new InvoiceService();
            var openInvoices = await invoiceService.ListAsync(
                new InvoiceListOptions { Subscription = stripeSubscriptionId.Value, Status = "open", Limit = 1 },
                GetRequestOptions(), cancellationToken
            );

            var invoice = openInvoices.Data.FirstOrDefault();
            if (invoice is null)
            {
                logger.LogInformation("No open invoices found for subscription '{SubscriptionId}'", stripeSubscriptionId);
                return null;
            }

            await invoiceService.PayAsync(invoice.Id, new InvoicePayOptions { PaymentMethod = paymentMethodId }, GetRequestOptions(), cancellationToken);

            logger.LogInformation("Retried payment for open invoice on subscription '{SubscriptionId}'", stripeSubscriptionId);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error retrying invoice payment for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout retrying invoice payment for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return false;
        }
    }

    public async Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(newPlan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan '{Plan}'", newPlan);
                return null;
            }

            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(stripeSubscriptionId.Value, requestOptions: GetRequestOptions(), cancellationToken: cancellationToken);
            var itemId = subscription.Items.Data.First().Id;

            var invoiceService = new InvoiceService();
            var invoice = await invoiceService.CreatePreviewAsync(new InvoiceCreatePreviewOptions
                {
                    Customer = subscription.CustomerId,
                    Subscription = stripeSubscriptionId.Value,
                    SubscriptionDetails = new InvoiceSubscriptionDetailsOptions
                    {
                        Items =
                        [
                            new InvoiceSubscriptionDetailsItemOptions { Id = itemId, Price = priceId }
                        ],
                        ProrationBehavior = "always_invoice"
                    }
                }, GetRequestOptions(), cancellationToken
            );

            var lineItems = invoice.Lines.Data
                .Select(line => new UpgradePreviewLineItem(
                        line.Description ?? "",
                        line.Amount / 100m,
                        line.Currency,
                        line.Parent?.InvoiceItemDetails?.Proration == true || line.Parent?.SubscriptionItemDetails?.Proration == true
                    )
                )
                .ToList();

            var totalTax = (invoice.TotalTaxes ?? []).Sum(t => t.Amount);
            if (totalTax > 0)
            {
                lineItems.Add(new UpgradePreviewLineItem("Tax", totalTax / 100m, invoice.Currency, false));
            }

            return new UpgradePreviewResult(invoice.AmountDue / 100m, invoice.Currency, lineItems.ToArray());
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting upgrade preview for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting upgrade preview for subscription '{SubscriptionId}'", stripeSubscriptionId);
            return null;
        }
    }

    public async Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        try
        {
            var priceId = GetPriceId(plan);
            if (priceId is null)
            {
                logger.LogError("Price ID not configured for plan '{Plan}'", plan);
                return null;
            }

            var invoiceService = new InvoiceService();
            var invoice = await invoiceService.CreatePreviewAsync(new InvoiceCreatePreviewOptions
                {
                    Customer = stripeCustomerId.Value,
                    SubscriptionDetails = new InvoiceSubscriptionDetailsOptions
                    {
                        Items = [new InvoiceSubscriptionDetailsItemOptions { Price = priceId }]
                    },
                    AutomaticTax = new InvoiceAutomaticTaxOptions { Enabled = true }
                }, GetRequestOptions(), cancellationToken
            );

            var totalTax = (invoice.TotalTaxes ?? []).Sum(t => t.Amount);

            logger.LogInformation("Generated checkout preview for customer '{CustomerId}' plan '{Plan}'", stripeCustomerId, plan);
            return new CheckoutPreviewResult(invoice.AmountDue / 100m, invoice.Currency, totalTax / 100m);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error getting checkout preview for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout getting checkout preview for customer '{CustomerId}'", stripeCustomerId);
            return null;
        }
    }

    private static string InferTaxIdType(string taxId)
    {
        var prefix = taxId[..2].ToUpperInvariant();
        return prefix switch
        {
            "GB" => "gb_vat",
            "NO" => "no_vat",
            "CH" => "ch_vat",
            _ => "eu_vat"
        };
    }

    private static string MapCancellationFeedback(CancellationReason reason)
    {
        return reason switch
        {
            CancellationReason.TooExpensive => "too_expensive",
            CancellationReason.FoundAlternative => "switched_service",
            CancellationReason.NoLongerNeeded => "unused",
            _ => "other"
        };
    }

    private static StripeCustomerId? ExtractCustomerId(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var dataObject = document.RootElement.GetProperty("data").GetProperty("object");

        if (dataObject.TryGetProperty("customer", out var customerProperty) && customerProperty.ValueKind == JsonValueKind.String)
        {
            var customerIdString = customerProperty.GetString();
            StripeCustomerId.TryParse(customerIdString, out var customerId);
            return customerId;
        }

        if (dataObject.TryGetProperty("object", out var objectType) && objectType.GetString() == "customer")
        {
            var customerIdString = dataObject.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
            StripeCustomerId.TryParse(customerIdString, out var customerId);
            return customerId;
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

    private static PaymentTransactionStatus MapInvoiceStatus(string? status, long amountPaid, long postPaymentCreditNotesAmount)
    {
        if (status == "paid" && amountPaid > 0 && postPaymentCreditNotesAmount >= amountPaid)
        {
            return PaymentTransactionStatus.Refunded;
        }

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
