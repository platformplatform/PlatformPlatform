using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class UpdateTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var command = new UpdateTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/tenants/{existingTenantId}", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("TenantUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var invalidName = Faker.Random.String2(31);
        var command = new UpdateTenantCommand { Name = invalidName };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/tenants/{existingTenantId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 30 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();
        var command = new UpdateTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/tenants/{unknownTenantId}", command);

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
