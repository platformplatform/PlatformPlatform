using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.BackOffice.Invoices.Queries;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class GetBackOfficeInvoicesTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetBackOfficeInvoices_WithoutStatusFilter_ShouldReturnEveryPaidAndRefundedRow()
    {
        // Arrange — three rows on Tenant1: paid, paid with credit note, refunded.
        SeedTransactions(
            Paid("inv_paid", "2025-01-01T00:00:00Z"),
            PaidWithCreditNote("inv_paid_cn", "2025-02-01T00:00:00Z", "2025-02-05T00:00:00Z"),
            Refunded("inv_refunded", "2025-03-01T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — no Statuses query param = "All" view.
        var response = await client.GetAsync("/api/back-office/invoices?PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithInvoicesStatusSet_ShouldReturnPaidRowsIncludingThoseWithCreditNote()
    {
        // Arrange
        SeedTransactions(
            Paid("inv_paid", "2025-01-01T00:00:00Z"),
            PaidWithCreditNote("inv_paid_cn", "2025-02-01T00:00:00Z", "2025-02-05T00:00:00Z"),
            Refunded("inv_refunded", "2025-03-01T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — "Invoices" view maps to Paid+Pending+Failed; a paid row with a credit note still matches Paid.
        var response = await client.GetAsync("/api/back-office/invoices?Statuses=Paid&Statuses=Pending&Statuses=Failed&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().Contain(i => i.CreditNoteUrl != null && i.Status == PaymentTransactionStatus.Succeeded);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithRefundsAndCreditNotesFilter_ShouldIncludeBothRefundedAndPaidWithCreditNote()
    {
        // Arrange
        SeedTransactions(
            Paid("inv_paid", "2025-01-01T00:00:00Z"),
            PaidWithCreditNote("inv_paid_cn", "2025-02-01T00:00:00Z", "2025-02-05T00:00:00Z"),
            Refunded("inv_refunded", "2025-03-01T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — "Refunds and credit notes" maps to Refunded OR HasCreditNote.
        var response = await client.GetAsync("/api/back-office/invoices?Statuses=Refunded&Statuses=HasCreditNote&PageSize=10");

        // Assert — both the refunded row AND the paid-with-credit-note row qualify.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().Contain(i => i.Status == PaymentTransactionStatus.Refunded);
        payload.Invoices.Should().Contain(i => i.CreditNoteUrl != null && i.Status == PaymentTransactionStatus.Succeeded);
    }

    private void SeedTransactions(params PaymentTransaction[] transactions)
    {
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("payment_transactions", JsonSerializer.Serialize(ImmutableArray.Create(transactions).ToArray()))
            ]
        );
    }

    private static PaymentTransaction Paid(string invoiceMarker, string isoDate)
    {
        return new PaymentTransaction(
            PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD",
            PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse(isoDate),
            null, $"https://stripe.test/{invoiceMarker}", null,
            SubscriptionPlan.Standard
        );
    }

    private static PaymentTransaction PaidWithCreditNote(string invoiceMarker, string isoDate, string creditNoteIsoDate)
    {
        return new PaymentTransaction(
            PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD",
            PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse(isoDate),
            null, $"https://stripe.test/{invoiceMarker}", $"https://stripe.test/{invoiceMarker}-cn",
            SubscriptionPlan.Standard, CreditNotedAt: DateTimeOffset.Parse(creditNoteIsoDate)
        );
    }

    private static PaymentTransaction Refunded(string invoiceMarker, string isoDate)
    {
        var date = DateTimeOffset.Parse(isoDate);
        return new PaymentTransaction(
            PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD",
            PaymentTransactionStatus.Refunded, date,
            null, $"https://stripe.test/{invoiceMarker}", $"https://stripe.test/{invoiceMarker}-cn",
            SubscriptionPlan.Standard, date.AddDays(3)
        );
    }
}
