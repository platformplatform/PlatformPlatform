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
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        object[] parameters = [new { id = subscriptionId }];
        var billingInfo = Connection.ExecuteScalar<string>("SELECT BillingInfo FROM Subscriptions WHERE Id = @id", parameters);
        billingInfo.Should().Contain("billing@example.com");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("BillingInfoUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenMultiLineAddress_ShouldSplitIntoLine1AndLine2()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12\nFloor 3", "1456", "Copenhagen", null, "DK", "billing@example.com", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        object[] parameters = [new { id = subscriptionId }];
        var billingInfo = Connection.ExecuteScalar<string>("SELECT BillingInfo FROM Subscriptions WHERE Id = @id", parameters);
        billingInfo.Should().Contain("Vestergade 12");
        billingInfo.Should().Contain("Floor 3");
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenNoStripeCustomer_ShouldCreateCustomerAndSucceed()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Basis)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", null),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new UpdateBillingInfoCommand("Test Organization", "Vestergade 12", "1456", "Copenhagen", null, "DK", "billing@example.com", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/subscriptions/billing-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        object[] parameters = [new { id = subscriptionId }];
        var billingInfo = Connection.ExecuteScalar<string>("SELECT BillingInfo FROM Subscriptions WHERE Id = @id", parameters);
        billingInfo.Should().Contain("billing@example.com");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("BillingInfoUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBillingInfo_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
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
