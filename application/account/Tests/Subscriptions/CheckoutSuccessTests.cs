using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Commands;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class CheckoutSuccessTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task CheckoutSuccess_WhenValidSession_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CheckoutSuccessCommand("cs_test_session_123");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/subscriptions/checkout-success", command);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
