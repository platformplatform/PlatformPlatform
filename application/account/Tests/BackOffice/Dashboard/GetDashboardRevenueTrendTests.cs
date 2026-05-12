using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardRevenueTrendTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardRevenueTrend_WhenThreeMonthsOfPayments_ShouldReturnMonthlyBucketsIncludingEmptyMonths()
    {
        // Arrange — seed payments across 3 months with the middle month empty so the gap-fill behaviour is exercised.
        var now = DateTimeOffset.UtcNow;
        var threeMonthsAgo = new DateTimeOffset(now.Year, now.Month, 15, 0, 0, 0, TimeSpan.Zero).AddMonths(-3);
        var thisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddDays(2);
        var tenant = SeedTenant("Paying Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, threeMonthsAgo, PaymentTransactionStatus.Succeeded, null),
            (149m, thisMonth, PaymentTransactionStatus.Succeeded, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Currency.Should().Be(MockStripeClient.MockStandardCurrency);
        payload.Points.Should().HaveCount(4); // 3 months ago, 2 months ago (empty), 1 month ago (empty), current month
        payload.Points[0].Revenue.Should().Be(149m);
        payload.Points[1].Revenue.Should().Be(0m);
        payload.Points[2].Revenue.Should().Be(0m);
        payload.Points[3].Revenue.Should().Be(149m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenTransactionsAreRefundedOrFailed_ShouldExcludeFromSum()
    {
        // Arrange — three payments in the same month: one succeeded, one refunded, one failed.
        var now = DateTimeOffset.UtcNow;
        var thisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddDays(2);
        var tenant = SeedTenant("Mixed Status Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, thisMonth, PaymentTransactionStatus.Succeeded, null),
            (149m, thisMonth, PaymentTransactionStatus.Refunded, thisMonth),
            (149m, thisMonth, PaymentTransactionStatus.Failed, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(1);
        payload.Points[0].Revenue.Should().Be(149m); // refunded + failed excluded
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenNoPaymentTransactions_ShouldReturnEmptyPoints()
    {
        // Arrange — no subscriptions seeded with transactions.
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private TenantId SeedTenant(string name)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddMonths(-6)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private void SeedSubscriptionWithTransactions(TenantId tenantId, SubscriptionId subscriptionId, params (decimal Amount, DateTimeOffset Date, PaymentTransactionStatus Status, DateTimeOffset? RefundedAt)[] transactions)
    {
        var paymentTransactions = JsonSerializer.Serialize(transactions.Select(t =>
                new PaymentTransaction(
                    PaymentTransactionId.NewId(),
                    t.Amount,
                    t.Amount,
                    0m,
                    MockStripeClient.MockStandardCurrency,
                    t.Status,
                    t.Date,
                    null,
                    null,
                    null,
                    SubscriptionPlan.Standard,
                    t.RefundedAt,
                    t.Amount
                )
            ).ToArray()
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddMonths(-6)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", $"cus_test_{tenantId.Value}"),
                ("stripe_subscription_id", $"sub_test_{tenantId.Value}"),
                ("current_price_amount", 149m),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", DateTimeOffset.UtcNow.AddMonths(1)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactions),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", DateTimeOffset.UtcNow.AddMonths(-3)),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }
}
