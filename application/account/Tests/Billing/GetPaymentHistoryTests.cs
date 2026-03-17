using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Billing.Queries;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Billing;

public sealed class GetPaymentHistoryTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetPaymentHistory_WhenTransactionsExist_ShouldReturnPaginatedHistory()
    {
        // Arrange
        var transactionId = PaymentTransactionId.NewId().ToString();
        var transactionsJson = $$"""[{"Id":"{{transactionId}}","Amount":29.99,"Currency":"usd","Status":"Succeeded","Date":"2026-01-01T00:00:00+00:00","FailureReason":null,"InvoiceUrl":"https://invoice.stripe.com/test"}]""";
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("payment_transactions", transactionsJson)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/billing/payment-history");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<PaymentHistoryResponse>();
        result!.TotalCount.Should().Be(1);
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Amount.Should().Be(29.99m);
        result.Transactions[0].Currency.Should().Be("usd");
        result.Transactions[0].Status.Should().Be(PaymentTransactionStatus.Succeeded);
    }

    [Fact]
    public async Task GetPaymentHistory_WhenNotOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/billing/payment-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
