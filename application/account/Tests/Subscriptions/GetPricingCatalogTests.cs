using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class GetPricingCatalogTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetPricingCatalog_WhenCalled_ShouldReturnPlans()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/pricing-catalog");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<PricingCatalogResponse>();
        result.Should().NotBeNull();
        result.Plans.Should().HaveCount(2);
        result.Plans.Should().AllSatisfy(plan =>
            {
                plan.UnitAmount.Should().BeGreaterThan(0);
                plan.Currency.Should().NotBeNullOrEmpty();
                plan.Interval.Should().NotBeNullOrEmpty();
                plan.IntervalCount.Should().BeGreaterThan(0);
            }
        );
    }

    [Fact]
    public async Task GetPricingCatalog_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account/subscriptions/pricing-catalog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
