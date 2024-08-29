using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Core.Database;
using PlatformPlatform.AccountManagement.Core.Signups.Commands;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Signups;

public sealed class SignupTests : BaseApiTests<AccountManagementDbContext>
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
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

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
        EnsureSuccessGetRequest(response);

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
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

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
        EnsureSuccessGetRequest(response);

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
        await EnsureSuccessPostRequest(response, hasLocation: false);
        Connection.RowExists("Tenants", signupId);
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "SignupCompleted").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "UserCreated").Should().Be(1);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
