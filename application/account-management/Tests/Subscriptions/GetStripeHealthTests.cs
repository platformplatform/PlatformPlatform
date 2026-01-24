using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Subscriptions;

public sealed class GetStripeHealthTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetStripeHealth_WhenCalled_ShouldReturnHealthStatus()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/subscriptions/stripe-health");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<StripeHealthResponse>();
        result.Should().NotBeNull();
    }
}
