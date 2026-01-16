using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
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
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(), [("Name", tenantName)]);

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
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(), [("Name", "Test Company")]);

        var existingUserEmail = DatabaseSeeder.Tenant1Owner.Email;
        var command = new InviteUserCommand(existingUserEmail);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"The user '{existingUserEmail}' already exists.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUser_WhenDeletedUserExists_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(), [("Name", "Test Company")]);

        var deletedUserEmail = Faker.Internet.UniqueEmail().ToLower();
        var deletedUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("Email", deletedUserEmail),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        var command = new InviteUserCommand(deletedUserEmail);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"The user '{deletedUserEmail}' was previously deleted. Please restore or permanently delete the user before inviting again.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
