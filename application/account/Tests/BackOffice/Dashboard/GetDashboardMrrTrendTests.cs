using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardMrrTrendTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardMrrTrend_WhenCalled_ShouldReturnDailyMrrSeriesForPeriod()
    {
        // Arrange — one paid subscription that has been running for 5 days, contributing 49.99 to MRR each day since.
        var now = DateTimeOffset.UtcNow;
        var paidTenant = SeedTenant("Paying Co");
        SeedActiveSubscription(paidTenant, 49.99m, now.AddDays(-5));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/mrr-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardMrrTrendResponse>();
        payload.Should().NotBeNull();
        payload.Period.Should().Be(DashboardTrendPeriod.Last7Days);
        payload.Currency.Should().Be("DKK");
        payload.Points.Should().HaveCount(7);
        // Subscription started 5 days ago, so the last 5 of 7 daily points include the price; the first 2 points predate
        // the subscription and remain at zero.
        payload.Points.Count(p => p.MonthlyRecurringRevenue == 49.99m).Should().BeGreaterOrEqualTo(4);
        payload.Points.Should().Contain(p => p.MonthlyRecurringRevenue == 0m);
        // Prior period covers the 7 days before the current window — the subscription did not exist yet, so MRR is zero.
        payload.PriorPoints.Should().HaveCount(7);
        payload.PriorPoints.Should().OnlyContain(p => p.MonthlyRecurringRevenue == 0m);
    }

    [Fact]
    public async Task GetDashboardMrrTrend_WhenCalledWithInvalidPeriod_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/mrr-trend?Period=NotAValidPeriod");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardMrrTrend_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/mrr-trend?Period=Last30Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private TenantId SeedTenant(string name)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private void SeedActiveSubscription(TenantId tenantId, decimal currentPriceAmount, DateTimeOffset subscribedSince)
    {
        var paymentTransactions = JsonSerializer.Serialize(new[]
            {
                new PaymentTransaction(PaymentTransactionId.NewId(), currentPriceAmount, "DKK", PaymentTransactionStatus.Succeeded, subscribedSince, null, null, null, SubscriptionPlan.Standard)
            }
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", subscribedSince),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", currentPriceAmount),
                ("current_price_currency", "DKK"),
                ("current_period_end", subscribedSince.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactions),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", subscribedSince),
                ("scheduled_price_amount", null)
            ]
        );
    }
}
