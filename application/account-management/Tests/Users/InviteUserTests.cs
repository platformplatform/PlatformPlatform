using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class InviteUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task InviteUser_WhenTenantNameNotSet_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();
        var command = new InviteUserCommand(email);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Account name must be set before inviting users.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUser_WhenTenantHasName_ShouldCreateUserAndUseTenantNameInEmail()
    {
        // Arrange
        var tenantName = "Test Company";
        // Update tenant name using SqliteConnectionExtensions
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(),
            [("Name", tenantName)]
        );

        var email = Faker.Internet.UniqueEmail();
        var command = new InviteUserCommand(email);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        // Verify user was created
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE TenantId = @tenantId AND Email = @email AND EmailConfirmed = 0",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.ToString(), email = email.ToLower() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserInvited");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailClient.Received(1).SendAsync(
            email.ToLower(),
            $"You have been invited to join {tenantName} on PlatformPlatform",
            Arg.Is<string>(s => s.Contains("To gain access")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task InviteUser_WhenInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidEmail = Faker.Internet.InvalidEmail();
        var command = new InviteUserCommand(invalidEmail);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUser_WhenUserExists_ShouldReturnBadRequest()
    {
        // Arrange
        // Set tenant name first (required for inviting users)
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(),
            [("Name", "Test Company")]
        );

        var existingUserEmail = DatabaseSeeder.Tenant1Owner.Email;
        var command = new InviteUserCommand(existingUserEmail);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"The user with '{existingUserEmail}' already exists.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
