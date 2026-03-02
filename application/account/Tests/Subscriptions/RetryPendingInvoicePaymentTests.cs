using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

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
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        MockStripeClient.SimulateOpenInvoice = true;

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/subscriptions/retry-pending-invoice", null);

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
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/subscriptions/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No pending invoice found for this subscription.");
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenNonOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/subscriptions/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");
    }

    [Fact]
    public async Task RetryPendingInvoicePayment_WhenNoStripeSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("StripeCustomerId", "cus_test_123")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/subscriptions/retry-pending-invoice", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No active Stripe subscription found.");
    }
}
