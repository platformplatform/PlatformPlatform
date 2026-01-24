using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Subscriptions.Domain;
using PlatformPlatform.AccountManagement.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Subscriptions;

public sealed class GetCurrentSubscriptionTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetCurrentSubscription_WhenExists_ShouldReturnSubscription()
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
                ("PaymentTransactions", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/subscriptions/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        result!.Plan.Should().Be(SubscriptionPlan.Standard);
        result.HasStripeSubscription.Should().BeTrue();
        result.CancelAtPeriodEnd.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentSubscription_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/subscriptions/current");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Subscription not found for current tenant.");
    }
}
