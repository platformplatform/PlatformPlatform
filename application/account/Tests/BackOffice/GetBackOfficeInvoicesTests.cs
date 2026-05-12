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
    public async Task GetBackOfficeInvoices_WithoutStatusFilter_ShouldReturnInvoiceAndCreditNoteRowsForEveryTransaction()
    {
        // Arrange — three rows on Tenant1: paid (no credit note), paid with credit note, refunded with credit note.
        // The two credit-noted rows each project to TWO summary rows (invoice + credit note); the plain paid row
        // projects to one. Expected total: 5.
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
        payload.TotalCount.Should().Be(5);
        payload.Invoices.Count(i => i.RowKind == BackOfficeInvoiceRowKind.Invoice).Should().Be(3);
        payload.Invoices.Count(i => i.RowKind == BackOfficeInvoiceRowKind.CreditNote).Should().Be(2);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithInvoicesStatusSet_ShouldReturnPaidInvoiceRowsIncludingThoseWithCreditNote()
    {
        // Arrange
        SeedTransactions(
            Paid("inv_paid", "2025-01-01T00:00:00Z"),
            PaidWithCreditNote("inv_paid_cn", "2025-02-01T00:00:00Z", "2025-02-05T00:00:00Z"),
            Refunded("inv_refunded", "2025-03-01T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — "Invoices" view maps to Paid+Pending+Failed; only RowKind=Invoice rows match.
        var response = await client.GetAsync("/api/back-office/invoices?Statuses=Paid&Statuses=Pending&Statuses=Failed&PageSize=10");

        // Assert — paid and paid-with-credit-note invoice rows match; refunded invoice doesn't; credit-note rows excluded.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().AllSatisfy(i => i.RowKind.Should().Be(BackOfficeInvoiceRowKind.Invoice));
        payload.Invoices.Should().Contain(i => i.CreditNoteUrl != null && i.Status == PaymentTransactionStatus.Succeeded);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithRefundsAndCreditNotesFilter_ShouldReturnCreditNoteRowsForCreditNotedTransactions()
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

        // Assert — both credit-note rows match; refunded invoice row is suppressed because its credit-note sibling
        // carries the refund signal.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().AllSatisfy(i => i.RowKind.Should().Be(BackOfficeInvoiceRowKind.CreditNote));
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WhenTransactionHasCreditNote_ShouldEmitTwoRowsSortedByOwnDate()
    {
        // Arrange — one paid transaction on 2025-02-01 with a credit note issued on 2025-02-05.
        var invoiceDate = "2025-02-01T00:00:00Z";
        var creditNoteDate = "2025-02-05T00:00:00Z";
        SeedTransactions(PaidWithCreditNote("inv_paid_cn", invoiceDate, creditNoteDate));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — descending by date is the default.
        var response = await client.GetAsync("/api/back-office/invoices?PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices[0].RowKind.Should().Be(BackOfficeInvoiceRowKind.CreditNote);
        payload.Invoices[0].Date.Should().Be(DateTimeOffset.Parse(creditNoteDate));
        payload.Invoices[1].RowKind.Should().Be(BackOfficeInvoiceRowKind.Invoice);
        payload.Invoices[1].Date.Should().Be(DateTimeOffset.Parse(invoiceDate));
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
            SubscriptionPlan.Standard, date.AddDays(3), CreditNotedAt: date.AddDays(3)
        );
    }
}
