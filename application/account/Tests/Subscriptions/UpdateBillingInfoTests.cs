using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class UpdateBillingInfoTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateBillingInfo_WhenValid_ShouldSucceed()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenMultiLineAddress_ShouldSplitIntoLine1AndLine2()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12\nFloor 3", "1456", "Copenhagen", null, "DK", "billing@example.com", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenNoStripeCustomer_ShouldCreateCustomerAndSucceed()
    {
        // Arrange
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage billing information.");
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenInvalidTaxId_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", "INVALID");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        var expectedErrors = new[] { new ErrorDetail("TaxId", "The provided Tax ID is not valid.") };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenRequiredFieldsEmpty_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new UpdateBillingInfoCommand("", "", "", "", null, "", "", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("name", "Name must be between 1 and 100 characters."),
            new ErrorDetail("address", "Address must be between 1 and 200 characters."),
            new ErrorDetail("postalCode", "Postal code must be between 1 and 10 characters."),
            new ErrorDetail("city", "City must be between 1 and 50 characters."),
            new ErrorDetail("country", "Country is required."),
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
