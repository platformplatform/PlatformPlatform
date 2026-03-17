using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Billing.Commands;
using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Billing;

[Collection("StripeTests")]
public sealed class RetryPendingInvoicePaymentTests : EndpointBaseTest<AccountDbContext>
{
    protected override void Dispose(bool disposing)
    {
        MockStripeClient.ResetOverrides();
        base.Dispose(disposing);
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenOpenInvoicePaid_ShouldReturnPaid()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        MockStripeClient.SimulateOpenInvoice = true;

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/billing/retry-pending-invoice", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RetryPendingInvoicePaymentResponse>();
        result!.Paid.Should().BeTrue();
        result.ClientSecret.Should().BeNull();
        result.PublishableKey.Should().BeNull();
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("PendingInvoicePaymentRetried");
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenNoOpenInvoice_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/billing/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No pending invoice found for this subscription.");
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenNonOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/billing/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenNoStripeSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", "cus_test_123")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/billing/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No active Stripe subscription found.");
    }
}
