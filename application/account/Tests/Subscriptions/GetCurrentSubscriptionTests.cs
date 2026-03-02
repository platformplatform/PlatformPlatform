using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class GetCurrentSubscriptionTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetCurrentSubscription_WhenExists_ShouldReturnSubscription()
    {
        // Arrange
        Connection.Update("Subscriptions", "TenantId", DatabaseSeeder.Tenant1.Id.Value, [
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPriceAmount", 29.99),
                ("CurrentPriceCurrency", "USD"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        result!.Plan.Should().Be(SubscriptionPlan.Standard);
        result.HasStripeSubscription.Should().BeTrue();
        result.CancelAtPeriodEnd.Should().BeFalse();
        result.CurrentPriceAmount.Should().Be(29.99m);
        result.CurrentPriceCurrency.Should().Be("USD");
    }
}
