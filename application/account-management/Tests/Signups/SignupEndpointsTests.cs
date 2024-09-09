using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Signups.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class SignupEndpointsTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartSignup_WhenTenantExists_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = DatabaseSeeder.Tenant1.Id;
        var command = new StartSignupCommand(subdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "The subdomain is not available.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubdomainFree_WhenTenantExists_ShouldReturnFalse()
    {
        // Arrange
        var subdomain = Faker.Subdomain();

        // Act
        var response = await AnonymousHttpClient
            .GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("true");
    }

    [Fact]
    public async Task StartSignup_WhenSubdomainInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var invalidSubdomain = Faker.Random.String(31);
        var command = new StartSignupCommand(invalidSubdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "Subdomain must be between 3 to 30 lowercase letters, numbers, or hyphens.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task IsSubdomainFree_WhenTenantExists_ShouldReturnTrue()
    {
        // Arrange
        var subdomain = DatabaseSeeder.Tenant1.Id;

        // Act
        var response =
            await AnonymousHttpClient.GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("false");
    }

    [Fact]
    public async Task CompleteSignup_WhenValid_ShouldCreateTenantAndOwnerUser()
    {
        // Arrange
        var email = DatabaseSeeder.Signup1.Email;
        var oneTimePassword = DatabaseSeeder.OneTimePassword;
        var command = new CompleteSignupCommand(oneTimePassword);
        var signupId = DatabaseSeeder.Signup1.Id;

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.RowExists("Tenants", signupId);
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "TenantCreated").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "SignupCompleted").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "UserCreated").Should().Be(1);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
