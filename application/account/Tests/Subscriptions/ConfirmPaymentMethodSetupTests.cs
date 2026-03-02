using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class ConfirmPaymentMethodSetupTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ConfirmPaymentMethodSetup_WhenValid_ShouldSucceed()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ConfirmPaymentMethodSetupCommand("seti_mock_12345");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/confirm-payment-method", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConfirmPaymentMethodSetupResponse>();
        result!.HasOpenInvoice.Should().BeFalse();
        result.OpenInvoiceAmount.Should().BeNull();
        result.OpenInvoiceCurrency.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmPaymentMethodSetup_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ConfirmPaymentMethodSetupCommand("seti_mock_12345");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/subscriptions/confirm-payment-method", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");
    }

    [Fact]
    public async Task ConfirmPaymentMethodSetup_WhenNoStripeCustomer_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new ConfirmPaymentMethodSetupCommand("seti_mock_12345");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/confirm-payment-method", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No Stripe customer found. A subscription must be created first.");
    }

    [Fact]
    public async Task ConfirmPaymentMethodSetup_WhenNoStripeSubscription_ShouldSetCustomerDefault()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("StripeCustomerId", "cus_test_123")
            ]
        );
        var command = new ConfirmPaymentMethodSetupCommand("seti_mock_12345");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/confirm-payment-method", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConfirmPaymentMethodSetupResponse>();
        result!.HasOpenInvoice.Should().BeFalse();
    }
}
