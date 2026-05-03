using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, "https://stripe.test/inv1", null),
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-02-01T00:00:00Z"), null, "https://stripe.test/inv2", null),
            new PaymentTransaction(PaymentTransactionId.NewId(), 29.00m, "USD", PaymentTransactionStatus.Failed, DateTimeOffset.Parse("2025-03-01T00:00:00Z"), "Card declined.", null, null)
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
