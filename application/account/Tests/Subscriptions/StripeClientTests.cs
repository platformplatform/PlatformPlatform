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
        // priceId exists on the invoice but is not in the catalog (e.g., archived price).
        // Arrange
        var invoice = BuildInvoiceWithPriceId("price_legacy_archived");
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId = new Dictionary<string, SubscriptionPlan>
        {
            ["price_standard"] = SubscriptionPlan.Standard
        };

        // Act
        var plan = AccountStripeClient.ResolvePlanForInvoice(invoice, planByPriceId);

        // historical / archived priceIds resolve to null rather than throwing or falling back to Basis.
        // Assert
        plan.Should().BeNull();
    }

    [Fact]
    public void ResolvePlanForInvoice_WithEmptyLookupTable_ShouldReturnNull()
    {
        // defensive case: the price catalog cache could legitimately be empty if Stripe is unreachable.
        // Arrange
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
        // invoice with no line items (rare but possible for zero-amount invoices).
        // Arrange
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
    public void ResolvePlanForInvoice_WithProrationUpgrade_ShouldReturnNewPlan()
    {
        // proration invoice for an upgrade has two lines: a negative credit on the old plan and a
        // positive charge on the new plan. Stripe may return the credit line first; we must still resolve
        // to the new plan being charged for, not the old one being credited.
        // Arrange
        var invoice = new Invoice
        {
            Lines = new StripeList<InvoiceLineItem>
            {
                Data =
                [
                    new InvoiceLineItem
                    {
                        Amount = -14654,
                        Pricing = new InvoiceLineItemPricing
                        {
                            PriceDetails = new InvoiceLineItemPricingPriceDetails { Price = "price_standard" }
                        }
                    },
                    new InvoiceLineItem
                    {
                        Amount = 29406,
                        Pricing = new InvoiceLineItemPricing
                        {
                            PriceDetails = new InvoiceLineItemPricingPriceDetails { Price = "price_premium" }
                        }
                    }
                ]
            }
        };
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
    public void ResolvePlanForInvoice_WithMissingPricing_ShouldReturnNull()
    {
        // line item without Pricing populated (e.g., manual line items).
        // Arrange
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

    [Fact]
    public void ComputeInvoiceAmountBreakdown_WhenZeroNetWithPositiveTax_ShouldClampAmountExcludingTaxAtZeroAndReportClamped()
    {
        // Stripe auto-tax can produce a zero-net paid invoice (proration credit fully offsets a new charge)
        // alongside a positive total_taxes. Without the clamp, AmountExcludingTax would go negative and silently
        // subtract from LTV. The Clamped flag surfaces the anomaly so callers can emit a warning + telemetry
        // and drift discrepancy without losing the row.
        // Arrange
        var invoice = new Invoice
        {
            Status = "paid",
            AmountPaid = 0,
            Total = 0,
            TotalTaxes = [new InvoiceTotalTax { Amount = 1611 }]
        };

        // Act
        var (displayAmount, amountExcludingTax, taxAmount, clamped) = AccountStripeClient.ComputeInvoiceAmountBreakdown(invoice);

        // Assert
        displayAmount.Should().Be(0m);
        amountExcludingTax.Should().Be(0m);
        taxAmount.Should().Be(16.11m);
        clamped.Should().BeTrue("display - tax = -16.11 was negative and got clamped");
    }

    [Fact]
    public void ComputeInvoiceAmountBreakdown_WhenPaidWithTax_ShouldSplitDisplayAmountAcrossTaxAndExcludingTax()
    {
        // normal paid invoice with positive amount and tax.
        // Arrange
        var invoice = new Invoice
        {
            Status = "paid",
            AmountPaid = 12500,
            Total = 12500,
            TotalTaxes = [new InvoiceTotalTax { Amount = 2500 }]
        };

        // Act
        var (displayAmount, amountExcludingTax, taxAmount, clamped) = AccountStripeClient.ComputeInvoiceAmountBreakdown(invoice);

        // Assert
        displayAmount.Should().Be(125m);
        amountExcludingTax.Should().Be(100m);
        taxAmount.Should().Be(25m);
        clamped.Should().BeFalse("display - tax = 100 is non-negative");
    }

    [Fact]
    public void MapInvoiceStatus_WhenVoid_ShouldReturnCancelled()
    {
        // A void invoice was never paid — no money ever changed hands. Mapping it to Refunded would be
        // incorrect (refund implies money returned) and would inflate refund counts.
        // Act
        var status = AccountStripeClient.MapInvoiceStatus("void", 0, 0);

        // Assert
        status.Should().Be(PaymentTransactionStatus.Cancelled);
    }

    [Fact]
    public void MapInvoiceStatus_WhenPaidWithoutRefund_ShouldReturnSucceeded()
    {
        // Act
        var status = AccountStripeClient.MapInvoiceStatus("paid", 10000, 0);

        // Assert
        status.Should().Be(PaymentTransactionStatus.Succeeded);
    }

    [Fact]
    public void MapInvoiceStatus_WhenPaidWithFullRefund_ShouldReturnRefunded()
    {
        // Act
        var status = AccountStripeClient.MapInvoiceStatus("paid", 10000, 10000);

        // Assert
        status.Should().Be(PaymentTransactionStatus.Refunded);
    }

    [Fact]
    public void MapInvoiceStatus_WhenOpen_ShouldReturnPending()
    {
        // Act
        var status = AccountStripeClient.MapInvoiceStatus("open", 0, 0);

        // Assert
        status.Should().Be(PaymentTransactionStatus.Pending);
    }

    [Fact]
    public void MapInvoiceStatus_WhenUncollectible_ShouldReturnFailed()
    {
        // Act
        var status = AccountStripeClient.MapInvoiceStatus("uncollectible", 0, 0);

        // Assert
        status.Should().Be(PaymentTransactionStatus.Failed);
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
