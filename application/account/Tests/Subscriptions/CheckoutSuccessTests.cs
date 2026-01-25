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

public sealed class CheckoutSuccessTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task CheckoutSuccess_WhenValidSession_ShouldSyncSubscription()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Trial)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]")
            ]
        );
        var command = new CheckoutSuccessCommand("cs_test_session_123");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/checkout-success", command);

        // Assert
        response.EnsureSuccessStatusCode();

        var plan = Connection.ExecuteScalar<string>("SELECT Plan FROM Subscriptions WHERE Id = @id", [new { id = subscriptionId }]);
        plan.Should().Be(nameof(SubscriptionPlan.Standard));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionCreated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CheckoutSuccess_WhenNoStripeCustomer_ShouldReturnBadRequest()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Trial)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", null),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]")
            ]
        );
        var command = new CheckoutSuccessCommand("cs_test_session_123");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/checkout-success", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No Stripe customer found for this subscription.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task CheckoutSuccess_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Trial)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]")
            ]
        );
        var command = new CheckoutSuccessCommand("cs_test_session_123");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/subscriptions/checkout-success", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
