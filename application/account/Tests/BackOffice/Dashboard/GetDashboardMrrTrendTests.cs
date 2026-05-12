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

public sealed class GetDashboardMrrTrendTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardMrrTrend_WhenCalled_ShouldReturnDailyMrrSeriesForPeriod()
    {
        // One paid subscription that has been running for 5 days, contributing 49.99 to MRR each day since.
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var paidTenant = SeedTenant("Paying Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedActiveSubscription(paidTenant, subscriptionId, 49.99m, now.AddDays(-5));
        SeedSubscriptionCreatedEvent(paidTenant, subscriptionId, 49.99m, now.AddDays(-5));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/mrr-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardMrrTrendResponse>();
        payload.Should().NotBeNull();
        payload.Period.Should().Be(DashboardTrendPeriod.Last7Days);
        payload.Currency.Should().Be(MockStripeClient.MockStandardCurrency);
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
    public async Task Handle_WhenTenantSoftDeletedAfterPayingForOneMonth_IncludesThatMonthInTrend()
    {
        // Historical-point semantic: a tenant paid for the month leading up to today, then was soft-deleted.
        // The MRR for the period it was paying must remain in the trend — billing events are immutable
        // historical money facts and the curve cannot rewrite itself when a tenant churns.
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var subscribedSince = now.AddDays(-5);
        var paidTenant = SeedTenant("Churned Paying Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedActiveSubscription(paidTenant, subscriptionId, 49.99m, subscribedSince);
        SeedSubscriptionCreatedEvent(paidTenant, subscriptionId, 49.99m, subscribedSince);
        Connection.Update("tenants", "id", paidTenant.Value, [("deleted_at", now)]);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/mrr-trend?Period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardMrrTrendResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().Contain(p => p.MonthlyRecurringRevenue == 49.99m, "the trend must include MRR for the period the now-soft-deleted tenant was paying");
        payload.Points.Count(p => p.MonthlyRecurringRevenue == 49.99m).Should().BeGreaterOrEqualTo(4);
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

    private void SeedActiveSubscription(TenantId tenantId, SubscriptionId subscriptionId, decimal currentPriceAmount, DateTimeOffset subscribedSince)
    {
        var paymentTransactions = JsonSerializer.Serialize(new[]
            {
                new PaymentTransaction(PaymentTransactionId.NewId(), currentPriceAmount, currentPriceAmount, 0m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, subscribedSince, null, null, null, SubscriptionPlan.Standard)
            }
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.ToString()),
                ("created_at", subscribedSince),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", currentPriceAmount),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", subscribedSince.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactions),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", subscribedSince),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }

    private void SeedSubscriptionCreatedEvent(TenantId tenantId, SubscriptionId subscriptionId, decimal newAmount, DateTimeOffset occurredAt)
    {
        Connection.Insert("billing_events", [
                ("tenant_id", tenantId.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", subscriptionId.ToString()),
                ("created_at", occurredAt),
                ("modified_at", null),
                ("stripe_event_id", $"evt_test_{Guid.NewGuid():N}"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Standard)),
                ("previous_amount", 0m),
                ("new_amount", newAmount),
                ("amount_delta", newAmount),
                ("committed_mrr", newAmount),
                ("currency", MockStripeClient.MockStandardCurrency),
                ("occurred_at", occurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );
    }
}
