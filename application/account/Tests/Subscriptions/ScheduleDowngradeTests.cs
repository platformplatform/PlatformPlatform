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

public sealed class ScheduleDowngradeTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ScheduleDowngrade_WhenPremiumToStandard_ShouldSucceed()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ScheduleDowngradeCommand(SubscriptionPlan.Standard);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/schedule-downgrade", command);

        // Assert
        response.EnsureSuccessStatusCode();

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleDowngrade_WhenPlanNotLower_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ScheduleDowngradeCommand(SubscriptionPlan.Premium);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/schedule-downgrade", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Cannot downgrade from 'Standard' to 'Premium'. Target plan must be lower.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleDowngrade_WhenTargetIsBasis_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ScheduleDowngradeCommand(SubscriptionPlan.Basis);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/schedule-downgrade", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Cannot downgrade to the Basis plan.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleDowngrade_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );
        var command = new ScheduleDowngradeCommand(SubscriptionPlan.Standard);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/subscriptions/schedule-downgrade", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
