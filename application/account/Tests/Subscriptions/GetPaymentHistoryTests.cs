using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class GetPaymentHistoryTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetPaymentHistory_WhenTransactionsExist_ShouldReturnPaginatedHistory()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        var transactionId = PaymentTransactionId.NewId().ToString();
        var transactionsJson = $$"""[{"Id":"{{transactionId}}","Amount":29.99,"Currency":"usd","Status":"Succeeded","Date":"2026-01-01T00:00:00+00:00","FailureReason":null,"InvoiceUrl":"https://invoice.stripe.com/test"}]""";
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("PaymentTransactions", transactionsJson)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/payment-history");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<PaymentHistoryResponse>();
        result!.TotalCount.Should().Be(1);
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Amount.Should().Be(29.99m);
        result.Transactions[0].Currency.Should().Be("usd");
        result.Transactions[0].Status.Should().Be(PaymentTransactionStatus.Succeeded);
    }
}
