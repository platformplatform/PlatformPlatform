using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.AccountRegistrations;

public sealed class AccountRegistrationsTests : BaseApiTests<AccountManagementDbContext>
{
    [Fact]
    public async Task CompleteAccountRegistration_WhenValid_ShouldCreateTenantAndOwnerUser()
    {
        // Arrange
        var email = DatabaseSeeder.AccountRegistration1.Email;
        var oneTimePassword = DatabaseSeeder.AccountRegistration1.OneTimePassword;
        var command = new CompleteAccountRegistrationCommand(oneTimePassword);
        var accountRegistrationId = DatabaseSeeder.AccountRegistration1.Id;

        // Act
        var response =
            await TestHttpClient.PostAsJsonAsync($"/api/account-registrations/{accountRegistrationId}/complete",
                command);

        // Assert
        await EnsureSuccessPostRequest(response, hasLocation: false);
        Connection.RowExists("Tenants", accountRegistrationId);
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "AccountRegistrationCompleted").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "UserCreated").Should().Be(1);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task StartAccountRegistration_WhenSubdomainInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var invalidSubdomain = Faker.Random.String(31);
        var command = new StartAccountRegistrationCommand(invalidSubdomain, email);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/account-registrations/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "Subdomain must be between 3-30 alphanumeric and lowercase characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task StartAccountRegistration_WhenTenantExists_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = DatabaseSeeder.Tenant1.Id;
        var command = new StartAccountRegistrationCommand(subdomain, email);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/account-registrations/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "The subdomain is not available.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}