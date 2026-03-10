using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class GetTenantDetailTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenantDetail_WhenTenantExists_ShouldReturnFullDetail()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-5)),
                ("modified_at", null),
                ("name", "Acme Corp"),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("logo", """{"Url":"https://example.com/logo.png","Version":1}"""),
                ("rollout_bucket", 50)
            ]
        );

        var billingInfoJson = JsonSerializer.Serialize(new BillingInfo("Acme Corp", new BillingAddress("123 Main St", null, "12345", "Springfield", "IL", "US"), null, null));
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(PaymentTransactionId.NewId(), 199.00m, 199.00m, 0m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, null, null, SubscriptionPlan.Premium, null, 199.00m)
        );
        var subscribedSince = DateTimeOffset.Parse("2025-02-01T00:00:00Z");
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-5)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", 199.00),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(25)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray())),
                ("payment_method", null),
                ("billing_info", billingInfoJson),
                ("subscribed_since", subscribedSince),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantDetailResponse>();
        payload.Should().NotBeNull();
        payload.Id.Should().Be(tenantId);
        payload.Name.Should().Be("Acme Corp");
        payload.Plan.Should().Be(SubscriptionPlan.Premium);
        payload.MonthlyRecurringRevenue.Should().Be(199.00m);
        payload.Currency.Should().Be(MockStripeClient.MockStandardCurrency);
        payload.LifetimeValue.Should().Be(199.00m);
        payload.BillingAddress.Should().NotBeNull();
        payload.BillingAddress.Country.Should().Be("US");
        payload.BillingAddress.City.Should().Be("Springfield");
        payload.LogoUrl.Should().Be("https://example.com/logo.png");
        payload.SubscribedSince.Should().Be(subscribedSince);
        payload.HasEverSubscribed.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantDetail_WhenSubscriptionMissing_ShouldReturnNullSubscribedSince()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-5)),
                ("modified_at", null),
                ("name", "No Subscription Inc"),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("logo", """{"Url":null,"Version":1}"""),
                ("rollout_bucket", 50)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantDetailResponse>();
        payload.Should().NotBeNull();
        payload.SubscribedSince.Should().BeNull();
        payload.HasEverSubscribed.Should().BeFalse();
    }

    [Fact]
    public async Task GetTenantDetail_WhenSubscriptionHasRefundedTransaction_ShouldExcludeRefundFromLifetimeValue()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-5)),
                ("modified_at", null),
                ("name", "Refunded Customer"),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("logo", """{"Url":null,"Version":1}"""),
                ("rollout_bucket", 50)
            ]
        );

        var transactions = ImmutableArray.Create(
            // Plain paid → counts
            new PaymentTransaction(PaymentTransactionId.NewId(), 100.00m, 80.00m, 20.00m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, null, null, SubscriptionPlan.Premium, null, 100.00m),
            // Refunded status → excluded
            new PaymentTransaction(PaymentTransactionId.NewId(), 100.00m, 80.00m, 20.00m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Refunded, DateTimeOffset.Parse("2025-02-01T00:00:00Z"), null, null, null, SubscriptionPlan.Premium, null, 100.00m),
            // Plain paid → counts
            new PaymentTransaction(PaymentTransactionId.NewId(), 100.00m, 80.00m, 20.00m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-03-01T00:00:00Z"), null, null, null, SubscriptionPlan.Premium, null, 100.00m),
            // Paid+credit-noted (Succeeded but later reversed by credit note) → excluded
            new PaymentTransaction(PaymentTransactionId.NewId(), 100.00m, 80.00m, 20.00m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-04-01T00:00:00Z"), null, null, "https://stripe.com/credit_note/test", SubscriptionPlan.Premium, null, 100.00m),
            // Paid+refunded-via-flag (Succeeded but RefundedAt set, no credit note) → excluded
            new PaymentTransaction(PaymentTransactionId.NewId(), 100.00m, 80.00m, 20.00m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-05-01T00:00:00Z"), null, null, null, SubscriptionPlan.Premium, DateTimeOffset.Parse("2025-05-15T00:00:00Z"), 100.00m)
        );
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-5)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test_refund"),
                ("stripe_subscription_id", "sub_test_refund"),
                ("current_price_amount", 100.00),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(25)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray())),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", DateTimeOffset.Parse("2025-01-01T00:00:00Z")),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantDetailResponse>();
        payload.Should().NotBeNull();
        // LTV sums AmountExcludingTax for transactions that are Succeeded AND not later reversed. Excluded:
        // the Refunded-status row, the credit-noted row, and the refunded-via-flag row. Two plain paid rows count.
        payload.LifetimeValue.Should().Be(160.00m);
    }

    [Fact]
    public async Task GetTenantDetail_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
