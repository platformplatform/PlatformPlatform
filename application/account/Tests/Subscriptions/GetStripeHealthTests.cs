using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class GetStripeHealthTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetStripeHealth_WhenCalled_ShouldReturnHealthStatus()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/stripe-health");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<StripeHealthResponse>();
        result.Should().NotBeNull();
    }
}
