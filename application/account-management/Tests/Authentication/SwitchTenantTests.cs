using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class SwitchTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task SwitchTenant_WhenUserExistsInTargetTenant_ShouldSwitchSuccessfully()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", tenant2Name),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("TenantSwitched");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.from_tenant_id"].Should().Be(DatabaseSeeder.Tenant1.Id.ToString());
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.to_tenant_id"].Should().Be(tenant2Id.ToString());
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(user2Id.ToString());
    }

    [Fact]
    public async Task SwitchTenant_WhenTargetTenantDoesNotExist_ShouldReturnForbidden()
    {
        // Arrange
        var nonExistentTenantId = TenantId.NewId();
        var command = new SwitchTenantCommand(nonExistentTenantId);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"User does not have access to tenant '{nonExistentTenantId}'.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SwitchTenant_WhenUserDoesNotExistInTargetTenant_ShouldReturnForbidden()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Owner)),
                ("Locale", "en-US")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"User does not have access to tenant '{tenant2Id}'.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SwitchTenant_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var command = new SwitchTenantCommand(TenantId.NewId());
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SwitchTenant_WhenUserEmailNotConfirmed_ShouldConfirmEmail()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", tenant2Name),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", false), // User's email is not confirmed
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        // Verify that the user's email is now confirmed
        var emailConfirmed = Connection.ExecuteScalar<long>(
            "SELECT EmailConfirmed FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        emailConfirmed.Should().Be(1); // SQLite stores boolean as 0/1
    }

    [Fact]
    public async Task SwitchTenant_WhenAcceptingInvite_ShouldCopyProfileData()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        // Current user has profile data
        var currentFirstName = Faker.Name.FirstName();
        var currentLastName = Faker.Name.LastName();
        var currentTitle = Faker.Name.JobTitle();
        var currentLocale = "da-DK";

        // Update current user with profile data
        Connection.Update("Users", "Id", DatabaseSeeder.Tenant1Member.Id.ToString(), [
                ("FirstName", currentFirstName),
                ("LastName", currentLastName),
                ("Title", currentTitle),
                ("Locale", currentLocale)
            ]
        );

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", tenant2Name),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        // New user has no profile data and unconfirmed email
        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", false), // Unconfirmed - invitation pending
                ("FirstName", null),
                ("LastName", null),
                ("Title", "Manager"), // Has a title that will be overwritten
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        // Verify profile data was copied
        var firstName = Connection.ExecuteScalar<string>(
            "SELECT FirstName FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var lastName = Connection.ExecuteScalar<string>(
            "SELECT LastName FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var title = Connection.ExecuteScalar<string>(
            "SELECT Title FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var locale = Connection.ExecuteScalar<string>(
            "SELECT Locale FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var emailConfirmed = Connection.ExecuteScalar<long>(
            "SELECT EmailConfirmed FROM Users WHERE Id = @Id",
            [new { Id = user2Id.ToString() }]
        );

        firstName.Should().Be(currentFirstName);
        lastName.Should().Be(currentLastName);
        title.Should().Be(currentTitle);
        locale.Should().Be(currentLocale);

        // Email should be confirmed
        emailConfirmed.Should().Be(1);
    }

    [Fact]
    public async Task SwitchTenant_RapidSwitching_ShouldHandleCorrectly()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response1 = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", new SwitchTenantCommand(tenant2Id)
        );
        var response2 = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", new SwitchTenantCommand(DatabaseSeeder.Tenant1.Id)
        );
        var response3 = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/authentication/switch-tenant", new SwitchTenantCommand(tenant2Id)
        );

        // Assert
        await response1.ShouldBeSuccessfulPostRequest(hasLocation: false);
        await response2.ShouldBeSuccessfulPostRequest(hasLocation: false);
        await response3.ShouldBeSuccessfulPostRequest(hasLocation: false);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(6);
        TelemetryEventsCollectorSpy.CollectedEvents.Where(e => e.GetType().Name == "SessionCreated").Should().HaveCount(3);
        TelemetryEventsCollectorSpy.CollectedEvents.Where(e => e.GetType().Name == "TenantSwitched").Should().HaveCount(3);
    }
}
