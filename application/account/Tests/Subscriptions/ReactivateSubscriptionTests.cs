using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class ReactivateSubscriptionTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ReactivateSubscription_WhenCancelledSamePlan_ShouldSucceed()
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
                ("CancelAtPeriodEnd", true),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Standard, null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactivateSubscriptionResponse>();
        result!.ClientSecret.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionReactivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenCancelledToHigherPlan_ShouldSucceed()
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
                ("CancelAtPeriodEnd", true),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Premium, null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactivateSubscriptionResponse>();
        result!.ClientSecret.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionReactivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenCancelledToLowerPlan_ShouldSucceed()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Premium)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", true),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Standard, null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactivateSubscriptionResponse>();
        result!.ClientSecret.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionReactivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenSuspended_ShouldReturnCheckoutUrl()
    {
        // Arrange
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.Value,
            [("State", nameof(TenantState.Suspended))]
        );
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Standard, "https://localhost:9000/subscription/return");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactivateSubscriptionResponse>();
        result!.ClientSecret.Should().NotBeNullOrEmpty();
        result.PublishableKey.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionReactivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenNotCancelled_ShouldReturnBadRequest()
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
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Standard, null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Subscription is not cancelled. Nothing to reactivate.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenNonOwner_ShouldReturnForbidden()
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
                ("CancelAtPeriodEnd", true),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        var command = new ReactivateSubscriptionCommand(SubscriptionPlan.Standard, null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
