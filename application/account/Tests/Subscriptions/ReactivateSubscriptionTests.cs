using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Subscriptions.Commands;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

public sealed class ReactivateSubscriptionTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ReactivateSubscription_WhenCancelled_ShouldSucceed()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("cancel_at_period_end", true)
            ]
        );
        var command = new ReactivateSubscriptionCommand();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactivateSubscriptionResponse>();
        result!.ClientSecret.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ReactivateSubscription_WhenNotCancelled_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ReactivateSubscriptionCommand();
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
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("cancel_at_period_end", true)
            ]
        );
        var command = new ReactivateSubscriptionCommand();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/subscriptions/reactivate", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
