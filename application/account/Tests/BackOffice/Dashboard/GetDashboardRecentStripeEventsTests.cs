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

public sealed class GetDashboardRecentStripeEventsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalled_ShouldEmitSubscribedAndUpgradedEvents()
    {
        // Arrange — one paid subscription with two successful payments at different prices. The handler should
        // emit a Subscribed event for the first payment and an Upgraded event for the second.
        var now = DateTimeOffset.UtcNow;
        var tenantId = SeedTenant("Stripe Co");
        SeedSubscriptionWithTwoPayments(tenantId, 49.99m, 79.99m, now.AddHours(-2));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRecentStripeEventsResponse>();
        payload.Should().NotBeNull();
        payload.Events.Should().Contain(e => e.Type == StripeEventType.Subscribed && e.TenantName == "Stripe Co");
        payload.Events.Should().Contain(e => e.Type == StripeEventType.Upgraded && e.TenantName == "Stripe Co");
        payload.Events[0].OccurredAt.Should().BeAfter(payload.Events[^1].OccurredAt);
    }

    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalledWithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=6");

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

    private void SeedSubscriptionWithTwoPayments(TenantId tenantId, decimal firstAmount, decimal secondAmount, DateTimeOffset secondAt)
    {
        var firstAt = secondAt.AddDays(-30);
        var paymentTransactions = JsonSerializer.Serialize(new[]
            {
                new PaymentTransaction(PaymentTransactionId.NewId(), firstAmount, "DKK", PaymentTransactionStatus.Succeeded, firstAt, null, null, null),
                new PaymentTransaction(PaymentTransactionId.NewId(), secondAmount, "DKK", PaymentTransactionStatus.Succeeded, secondAt, null, null, null)
            }
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", firstAt),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", secondAmount),
                ("current_price_currency", "DKK"),
                ("current_period_end", secondAt.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactions),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", firstAt),
                ("scheduled_price_amount", null)
            ]
        );
    }
}
