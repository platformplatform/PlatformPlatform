using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class DeleteTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account-management/tenants/{unknownTenantId}");

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasUsers_ShouldReturnBadRequest()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.Email()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", UserRole.Member.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account-management/tenants/{existingTenantId}");
        TelemetryEventsCollectorSpy.Reset();

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Id", "All users must be deleted before the tenant can be deleted.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasNoUsers_ShouldDeleteTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account-management/tenants/{existingTenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Tenants", existingTenantId).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
