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

        // Assert — all three invoice rows match (paid, paid-with-credit-note, AND the refunded transaction's
        // invoice row whose status is now Succeeded since the original payment outcome was successful).
        // Credit-note rows excluded.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(3);
        payload.Invoices.Should().AllSatisfy(i => i.RowKind.Should().Be(BackOfficeInvoiceRowKind.Invoice));
        payload.Invoices.Should().AllSatisfy(i => i.Status.Should().Be(PaymentTransactionStatus.Succeeded));
        payload.Invoices.Should().Contain(i => i.CreditNoteUrl != null);
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

        // Assert — both transactions have a credit note, so both project to a CreditNote row.
        // No standalone Refund rows because the credit note encompasses the refund in that case.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().AllSatisfy(i => i.RowKind.Should().Be(BackOfficeInvoiceRowKind.CreditNote));
    }

    [Fact]
    public async Task GetBackOfficeInvoices_InvoiceRow_ShouldShowOriginalPaymentOutcomeNotRefundedStatus()
    {
        // Arrange — a refunded transaction. The Invoice row must show Status=Succeeded (Paid) since
        // the original payment succeeded; only the reversal row carries the Refunded signal.
        SeedTransactions(Refunded("inv_refunded", "2025-03-01T00:00:00Z"));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/invoices?PageSize=10");

        // Assert — 2 rows: Invoice (Succeeded — not Refunded) + CreditNote.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        var invoiceRow = payload.Invoices.Single(i => i.RowKind == BackOfficeInvoiceRowKind.Invoice);
        invoiceRow.Status.Should().Be(PaymentTransactionStatus.Succeeded);
        payload.Invoices.Should().Contain(i => i.RowKind == BackOfficeInvoiceRowKind.CreditNote);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WhenRefundWithoutCreditNote_ShouldEmitRefundRow()
    {
        // Arrange — Stripe pro-rated refund edge case: a refund without an accompanying credit note.
        SeedTransactions(RefundedWithoutCreditNote("inv_refund_only", "2025-04-01T00:00:00Z", "2025-04-05T00:00:00Z"));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/invoices?PageSize=10");

        // Assert — Invoice (Succeeded) + Refund (no CreditNote row since CreditNoteUrl is null).
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().Contain(i => i.RowKind == BackOfficeInvoiceRowKind.Invoice && i.Status == PaymentTransactionStatus.Succeeded);
        payload.Invoices.Should().Contain(i => i.RowKind == BackOfficeInvoiceRowKind.Refund);
        payload.Invoices.Should().NotContain(i => i.RowKind == BackOfficeInvoiceRowKind.CreditNote);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithRefundedFilter_ShouldReturnRefundRowsOnly()
    {
        // Arrange — one credit-noted (renders as CreditNote row) and one refund-only (renders as Refund row).
        SeedTransactions(
            PaidWithCreditNote("inv_paid_cn", "2025-02-01T00:00:00Z", "2025-02-05T00:00:00Z"),
            RefundedWithoutCreditNote("inv_refund_only", "2025-04-01T00:00:00Z", "2025-04-05T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act — Refunded filter alone matches only RowKind=Refund.
        var response = await client.GetAsync("/api/back-office/invoices?Statuses=Refunded&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Invoices[0].RowKind.Should().Be(BackOfficeInvoiceRowKind.Refund);
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WhenLegacyTransactionHasCreditNoteUrlButNullDates_ShouldStillProjectTwoRowsDatedByPaymentDate()
    {
        // Arrange — a legacy refunded transaction whose CreditNoteUrl is set but whose CreditNotedAt and
        // RefundedAt were never backfilled (pre-fix data). The projection should still surface a credit-note
        // row at the only timestamp available — the original payment date.
        var paymentDate = "2025-04-01T00:00:00Z";
        SeedTransactions(RefundedWithLegacyNullDates("inv_legacy", paymentDate));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/invoices?PageSize=10");

        // Assert — two rows: Invoice (Succeeded — original payment outcome) and CreditNote, both dated by the original payment date.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Invoices.Should().Contain(i => i.RowKind == BackOfficeInvoiceRowKind.Invoice && i.Status == PaymentTransactionStatus.Succeeded);
        payload.Invoices.Should().Contain(i => i.RowKind == BackOfficeInvoiceRowKind.CreditNote);
        payload.Invoices.Should().AllSatisfy(i => i.Date.Should().Be(DateTimeOffset.Parse(paymentDate)));
    }

    [Fact]
    public async Task GetBackOfficeInvoices_WithRefundsAndCreditNotesFilter_ShouldIncludeLegacyCreditNotedRows()
    {
        // Arrange — a legacy refunded transaction (CreditNoteUrl set, dates null) plus a plain paid invoice.
        // The Refunds-and-credit-notes filter must surface the legacy credit-note row, not return empty.
        SeedTransactions(
            Paid("inv_paid", "2025-04-01T00:00:00Z"),
            RefundedWithLegacyNullDates("inv_legacy", "2025-04-15T00:00:00Z")
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/invoices?Statuses=Refunded&Statuses=HasCreditNote&PageSize=10");

        // Assert — exactly the credit-note row from the legacy transaction.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeInvoicesResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Invoices[0].RowKind.Should().Be(BackOfficeInvoiceRowKind.CreditNote);
        payload.Invoices[0].CreditNoteUrl.Should().NotBeNull();
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

    private static PaymentTransaction RefundedWithLegacyNullDates(string invoiceMarker, string isoDate)
    {
        // Mirrors pre-fix production data: a credit-noted refund where neither RefundedAt nor
        // CreditNotedAt were populated by the producer. Used to verify the projection's date fallback.
        return new PaymentTransaction(
            PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD",
            PaymentTransactionStatus.Refunded, DateTimeOffset.Parse(isoDate),
            null, $"https://stripe.test/{invoiceMarker}", $"https://stripe.test/{invoiceMarker}-cn",
            SubscriptionPlan.Standard
        );
    }

    private static PaymentTransaction RefundedWithoutCreditNote(string invoiceMarker, string isoDate, string refundIsoDate)
    {
        // Stripe pro-rated refund edge case: a refund happens without an accompanying credit note.
        // Used to verify the projection emits a standalone Refund row.
        return new PaymentTransaction(
            PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD",
            PaymentTransactionStatus.Refunded, DateTimeOffset.Parse(isoDate),
            null, $"https://stripe.test/{invoiceMarker}", null,
            SubscriptionPlan.Standard, DateTimeOffset.Parse(refundIsoDate)
        );
    }
}
