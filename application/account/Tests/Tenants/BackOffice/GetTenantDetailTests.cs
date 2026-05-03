using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Tenants.Domain;
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
                ("logo", """{"Url":"https://example.com/logo.png","Version":1}"""),
                ("plan", nameof(SubscriptionPlan.Premium))
            ]
        );

        var billingInfoJson = JsonSerializer.Serialize(new BillingInfo("Acme Corp", new BillingAddress("123 Main St", null, "12345", "Springfield", "IL", "US"), null, null));
        var transactions = ImmutableArray.Create(
            new PaymentTransaction(PaymentTransactionId.NewId(), 199.00m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, null, null)
        );
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
                ("current_price_currency", "USD"),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(25)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", JsonSerializer.Serialize(transactions.ToArray())),
                ("payment_method", null),
                ("billing_info", billingInfoJson)
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
        payload.Currency.Should().Be("USD");
        payload.LifetimeValue.Should().Be(199.00m);
        payload.BillingAddress.Should().NotBeNull();
        payload.BillingAddress.Country.Should().Be("US");
        payload.BillingAddress.City.Should().Be("Springfield");
        payload.LogoUrl.Should().Be("https://example.com/logo.png");
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
