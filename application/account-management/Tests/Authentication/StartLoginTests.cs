using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class StartLoginTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartLogin_WhenValidEmailAndUserExists_ShouldReturnSuccess()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;
        var command = new StartLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartLoginResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailClient.Received(1).SendAsync(
            email.ToLower(),
            "PlatformPlatform login verification code",
            Arg.Is<string>(s => s.Contains("Your confirmation code is below")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartLoginCommand_WhenEmailIsEmpty_ShouldFail()
    {
        // Arrange
        var command = new StartLoginCommand("");

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "'Email' must not be empty.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Invalid Email Format", "invalid-email")]
    [InlineData("Email Too Long", "abcdefghijklmnopqrstuvwyz0123456789-abcdefghijklmnopqrstuvwyz0123456789-abcdefghijklmnopqrstuvwyz0123456789@example.com")]
    public async Task StartLoginCommand_WhenEmailInvalid_ShouldFail(string scenario, string invalidEmail)
    {
        // Arrange
        var command = new StartLoginCommand(invalidEmail);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse(scenario);
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartLoginCommand_WhenUserDoesNotExist_ShouldReturnFakeLoginId()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartLoginResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();

        await EmailClient.Received(1).SendAsync(
            email.ToLower(),
            "Unknown user tried to login to PlatformPlatform",
            Arg.Is<string>(s => s.Contains("You or someone else tried to login to PlatformPlatform")),
            Arg.Any<CancellationToken>()
        );
    }
}
