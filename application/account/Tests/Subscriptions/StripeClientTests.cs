using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using Stripe;
using Xunit;
using AccountStripeClient = Account.Integrations.Stripe.StripeClient;

namespace Account.Tests.Subscriptions;

// Pins the Stripe SDK path used to extract the priceId from an invoice's first line item, plus the lookup
// fallback for unknown / archived priceIds. The full SyncPaymentTransactionsAsync flow is exercised
// indirectly by webhook integration tests via MockStripeClient; these tests gate the SDK-coupling itself
// (Invoice.Lines.Data[].Pricing.PriceDetails.Price) so that an SDK upgrade or wrong-property reference
// surfaces as a unit-test failure rather than silently producing PaymentTransaction.Plan = null in production.
public sealed class StripeClientTests
{
    [Fact]
    public void ResolvePlanForInvoice_WithKnownPriceId_ShouldReturnMappedPlan()
    {
        // Arrange
        var invoice = BuildInvoiceWithPriceId("price_standard");
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard,
            ["price_premium"] = SubscriptionPlan.Premium
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert
        plan.Should().Be(SubscriptionPlan.Standard);
    }

    [Fact]
    public void ResolvePlanForInvoice_WithPremiumPriceId_ShouldReturnPremium()
    {
        // Arrange
        var invoice = BuildInvoiceWithPriceId("price_premium");
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard,
            ["price_premium"] = SubscriptionPlan.Premium
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert
        plan.Should().Be(SubscriptionPlan.Premium);
    }

    [Fact]
    public void ResolvePlanForInvoice_WithUnknownPriceId_ShouldReturnNull()
    {
        // Arrange — priceId exists on the invoice but is not in the catalog (e.g., archived price).
        var invoice = BuildInvoiceWithPriceId("price_legacy_archived");
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert — historical / archived priceIds resolve to null rather than throwing or falling back to Basis.
        plan.Should().BeNull();
    }

    [Fact]
    public void ResolvePlanForInvoice_WithEmptyLookupTable_ShouldReturnNull()
    {
        // Arrange — defensive case: the price catalog cache could legitimately be empty if Stripe is unreachable.
        var invoice = BuildInvoiceWithPriceId("price_standard");
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>();

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert
        plan.Should().BeNull();
    }

    [Fact]
    public void ResolvePlanForInvoice_WithoutLineItems_ShouldReturnNull()
    {
        // Arrange — invoice with no line items (rare but possible for zero-amount invoices).
        var invoice = new Invoice { Lines = new StripeList<InvoiceLineItem> { Data = [] } };
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert
        plan.Should().BeNull();
    }

    [Fact]
    public void ResolvePlanForInvoice_WithMissingPricing_ShouldReturnNull()
    {
        // Arrange — line item without Pricing populated (e.g., manual line items).
        var invoice = new Invoice
        {
            Lines = new StripeList<InvoiceLineItem>
            {
                Data = [new InvoiceLineItem { Pricing = null }]
            }
        };
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // Assert
        plan.Should().BeNull();
    }

    private static Invoice BuildInvoiceWithPriceId(string priceId)
    {
        return new Invoice
        {
            Lines = new StripeList<InvoiceLineItem>
            {
                Data =
                [
                    new InvoiceLineItem
                    {
                        Pricing = new InvoiceLineItemPricing
                        {
                            PriceDetails = new InvoiceLineItemPricingPriceDetails { Price = priceId }
                        }
                    }
                ]
            }
        };
    }
}
