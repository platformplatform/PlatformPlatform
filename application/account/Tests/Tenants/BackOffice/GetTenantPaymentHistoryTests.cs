using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.BackOffice.Invoices.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class GetTenantPaymentHistoryTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenantPaymentHistory_WhenSubscriptionHasTransactions_ShouldReturnPagedTransactions()
    {
        // Arrange — three transactions, none with credit notes, so each projects to a single Invoice row.
        var tenant = DatabaseSeeder.Tenant1;
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, "https://stripe.test/inv1", null, SubscriptionPlan.Standard),
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-02-01T00:00:00Z"), null, "https://stripe.test/inv2", null, SubscriptionPlan.Standard),
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD", PaymentTransactionStatus.Failed, DateTimeOffset.Parse("2025-03-01T00:00:00Z"), "Card declined.", null, null, SubscriptionPlan.Standard)
        );
        Connection.Update("subscriptions", "tenant_id", tenant.Id.Value, [
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray()))
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/payment-history?pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantPaymentHistoryResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(3);
        payload.Transactions.Should().HaveCount(2);
        payload.Transactions[0].Date.Should().BeAfter(payload.Transactions[1].Date);
        payload.Transactions[0].Status.Should().Be(PaymentTransactionStatus.Failed);
        payload.Transactions[0].Plan.Should().Be(SubscriptionPlan.Standard);
        payload.Transactions.Should().AllSatisfy(t => t.RowKind.Should().Be(BackOfficeInvoiceRowKind.Invoice));
    }

    [Fact]
    public async Task GetTenantPaymentHistory_WhenTransactionHasCreditNote_ShouldEmitTwoRowsSortedByOwnDate()
    {
        // Arrange — a refunded transaction with a credit note issued three days after the original invoice.
        var tenant = DatabaseSeeder.Tenant1;
        var invoiceDate = DateTimeOffset.Parse("2025-01-01T00:00:00Z");
        var creditNoteDate = DateTimeOffset.Parse("2025-01-04T10:30:00Z");
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(
                PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD", PaymentTransactionStatus.Refunded,
                invoiceDate, null, "https://stripe.test/inv-cn", "https://stripe.test/cn-pdf",
                SubscriptionPlan.Standard, creditNoteDate, CreditNotedAt: creditNoteDate
            )
        );
        Connection.Update("subscriptions", "tenant_id", tenant.Id.Value, [
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray()))
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/payment-history");

        // Assert — invoice row at invoiceDate AND credit-note row at creditNoteDate, descending so credit note first.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantPaymentHistoryResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Transactions.Should().HaveCount(2);
        payload.Transactions[0].RowKind.Should().Be(BackOfficeInvoiceRowKind.CreditNote);
        payload.Transactions[0].Date.Should().Be(creditNoteDate);
        payload.Transactions[0].Status.Should().Be(PaymentTransactionStatus.Refunded);
        payload.Transactions[0].CreditNoteUrl.Should().Be("https://stripe.test/cn-pdf");
        payload.Transactions[1].RowKind.Should().Be(BackOfficeInvoiceRowKind.Invoice);
        payload.Transactions[1].Date.Should().Be(invoiceDate);
        payload.Transactions[1].Status.Should().Be(PaymentTransactionStatus.Succeeded);
        payload.Transactions[1].CreditNotedAt.Should().Be(creditNoteDate);
    }

    [Fact]
    public async Task GetTenantPaymentHistory_WhenRefundWithoutCreditNote_ShouldEmitInvoiceAndRefundRows()
    {
        // Arrange — Stripe pro-rated refund edge case (Symphonic-style): refund without a credit note.
        var tenant = DatabaseSeeder.Tenant1;
        var invoiceDate = DateTimeOffset.Parse("2025-04-01T00:00:00Z");
        var refundDate = DateTimeOffset.Parse("2025-04-05T00:00:00Z");
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(
                PaymentTransactionId.NewId(), 29.00m, 29.00m, 0m, "USD", PaymentTransactionStatus.Refunded,
                invoiceDate, null, "https://stripe.test/inv-refund-only", null,
                SubscriptionPlan.Standard, refundDate
            )
        );
        Connection.Update("subscriptions", "tenant_id", tenant.Id.Value, [
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray()))
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/payment-history");

        // Assert — Invoice (Succeeded) + Refund (no CreditNote row since CreditNoteUrl is null).
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantPaymentHistoryResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2);
        payload.Transactions.Should().Contain(t => t.RowKind == BackOfficeInvoiceRowKind.Invoice && t.Status == PaymentTransactionStatus.Succeeded && t.Date == invoiceDate);
        payload.Transactions.Should().Contain(t => t.RowKind == BackOfficeInvoiceRowKind.Refund && t.Status == PaymentTransactionStatus.Refunded && t.Date == refundDate);
        payload.Transactions.Should().NotContain(t => t.RowKind == BackOfficeInvoiceRowKind.CreditNote);
    }

    [Fact]
    public async Task GetTenantPaymentHistory_WhenSubscriptionHasNoTransactions_ShouldReturnEmpty()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/payment-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantPaymentHistoryResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(0);
        payload.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenantPaymentHistory_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/payment-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
