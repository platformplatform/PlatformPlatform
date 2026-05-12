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
    public async Task GetDashboardRevenueTrend_WhenPaymentsAcrossMultipleDays_ShouldAccumulateRevenueInPeriod()
    {
        // Arrange — two payments inside the last 7 days (-3 and today). Curve cumulates from zero to 298.
        var today = DateTimeOffset.UtcNow.Date;
        var threeDaysAgo = new DateTimeOffset(today.AddDays(-3), TimeSpan.Zero).AddHours(9);
        var todayTimestamp = new DateTimeOffset(today, TimeSpan.Zero).AddHours(9);
        var tenant = SeedTenant("Paying Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, threeDaysAgo, PaymentTransactionStatus.Succeeded, null),
            (149m, todayTimestamp, PaymentTransactionStatus.Succeeded, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Period.Should().Be(DashboardTrendPeriod.Last7Days);
        payload.Currency.Should().Be(MockStripeClient.MockStandardCurrency);
        payload.Points.Should().HaveCount(7);
        // Day 0 (today - 6 days) through Day 2 (today - 4 days): no deltas — cumulative stays at zero.
        payload.Points[0].Revenue.Should().Be(0m);
        payload.Points[1].Revenue.Should().Be(0m);
        payload.Points[2].Revenue.Should().Be(0m);
        // Day 3 (today - 3 days): +149 — cumulative reaches 149.
        payload.Points[3].Revenue.Should().Be(149m);
        payload.Points[4].Revenue.Should().Be(149m);
        payload.Points[5].Revenue.Should().Be(149m);
        // Day 6 (today): another +149 — cumulative reaches 298.
        payload.Points[6].Revenue.Should().Be(298m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenTransactionIsRefundedOnLaterDay_ShouldDipOnRefundDate()
    {
        // Arrange — one transaction paid on day -5 and refunded on day -1. Cumulative jumps to 149 on the paid day
        // and drops back to zero on the refund day.
        var today = DateTimeOffset.UtcNow.Date;
        var paidOn = new DateTimeOffset(today.AddDays(-5), TimeSpan.Zero).AddHours(9);
        var refundedOn = new DateTimeOffset(today.AddDays(-1), TimeSpan.Zero).AddHours(15);
        var tenant = SeedTenant("Refunded Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, paidOn, PaymentTransactionStatus.Refunded, refundedOn)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(7);
        // Day 0 (today - 6 days) — empty.
        payload.Points[0].Revenue.Should().Be(0m);
        // Day 1 (today - 5 days) — paid, cumulative reaches 149.
        payload.Points[1].Revenue.Should().Be(149m);
        payload.Points[2].Revenue.Should().Be(149m);
        payload.Points[3].Revenue.Should().Be(149m);
        payload.Points[4].Revenue.Should().Be(149m);
        // Day 5 (today - 1 day) — refunded, cumulative drops back to zero.
        payload.Points[5].Revenue.Should().Be(0m);
        payload.Points[6].Revenue.Should().Be(0m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenFailedTransactions_ShouldBeExcludedFromCumulative()
    {
        // Arrange — one succeeded and one failed on the same day; only succeeded contributes.
        var today = DateTimeOffset.UtcNow.Date;
        var todayTimestamp = new DateTimeOffset(today, TimeSpan.Zero).AddHours(9);
        var tenant = SeedTenant("Mixed Status Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, todayTimestamp, PaymentTransactionStatus.Succeeded, null),
            (149m, todayTimestamp, PaymentTransactionStatus.Failed, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(7);
        // Only the most recent day (today) carries the 149 delta — cumulative jumps to 149 on the last point.
        payload.Points[6].Revenue.Should().Be(149m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenPaymentInPriorWindow_ShouldRollForwardIntoCurrentSeriesStart()
    {
        // Arrange — a payment in the prior 7-day window AND a payment inside the current window. The cumulative
        // is "all-time through this day" not "within window", so the prior-window payment must already be
        // reflected on day one of the current series.
        var today = DateTimeOffset.UtcNow.Date;
        var priorWindowPayment = new DateTimeOffset(today.AddDays(-10), TimeSpan.Zero).AddHours(9);
        var currentWindowPayment = new DateTimeOffset(today.AddDays(-2), TimeSpan.Zero).AddHours(9);
        var tenant = SeedTenant("Two Windows Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, priorWindowPayment, PaymentTransactionStatus.Succeeded, null),
            (149m, currentWindowPayment, PaymentTransactionStatus.Succeeded, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(7);
        payload.PriorPoints.Should().HaveCount(7);
        // Current window day 0 already carries the 149 from the prior-window payment; the day -2 payment lifts
        // it to 298 for the remainder of the window — last point equals the all-time total.
        payload.Points[0].Revenue.Should().Be(149m);
        payload.Points[3].Revenue.Should().Be(149m);
        payload.Points[4].Revenue.Should().Be(298m);
        payload.Points[^1].Revenue.Should().Be(298m);
        // Prior window: no revenue before priorStartDate (today - 13), so day 0 is zero. On day 3 of the prior
        // window (today - 10) the prior-window payment lifts it to 149 and it stays there.
        payload.PriorPoints[0].Revenue.Should().Be(0m);
        payload.PriorPoints[^1].Revenue.Should().Be(149m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenRevenueAccumulatedLongBeforeCurrentWindow_ShouldStartCurrentSeriesAtAllTimeCumulative()
    {
        // Arrange — single payment 20 days ago, before both the current 7-day window and its prior window. The
        // current series should already report 149 on day one and stay there through today (the all-time total).
        var today = DateTimeOffset.UtcNow.Date;
        var ancientPayment = new DateTimeOffset(today.AddDays(-20), TimeSpan.Zero).AddHours(9);
        var tenant = SeedTenant("Ancient History Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedSubscriptionWithTransactions(tenant, subscriptionId,
            (149m, ancientPayment, PaymentTransactionStatus.Succeeded, null)
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(7);
        payload.Points.Should().OnlyContain(p => p.Revenue == 149m);
        payload.PriorPoints.Should().HaveCount(7);
        // The 20-day-ago payment is also before priorStartDate (today - 13), so the prior series also reports
        // the constant all-time-through-prior-day total.
        payload.PriorPoints.Should().OnlyContain(p => p.Revenue == 149m);
    }

    [Fact]
    public async Task GetDashboardRevenueTrend_WhenNoPaymentTransactions_ShouldReturnZeroFilledSeries()
    {
        // Arrange — no subscriptions seeded with transactions; both series fill the period with zeros.
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/revenue-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRevenueTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(7);
        payload.Points.Should().OnlyContain(p => p.Revenue == 0m);
        payload.PriorPoints.Should().HaveCount(7);
        payload.PriorPoints.Should().OnlyContain(p => p.Revenue == 0m);
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
