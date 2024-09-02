using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Core.Database;
using PlatformPlatform.AccountManagement.Core.Tenants.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class TenantEndpointsTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenantWithValidContract()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/tenants/{existingTenantId}");

        // Assert
        ApiTestHelpers.EnsureSuccessGetRequest(response);

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'string', 'pattern': '^[a-z0-9]{3,30}$'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'name': {'type': 'string', 'minLength': 1, 'maxLength': 30},
                    'state': {'type': 'string', 'minLength': 1, 'maxLength':20}
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'name', 'state'],
                'additionalProperties': false
            }
            """
        );

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/tenants/{unknownTenantId}");

        // Assert
        await ApiTestHelpers.EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");
    }

    [Fact]
    public async Task GetTenant_WhenTenantInvalidTenantId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidTenantId = Faker.Random.AlphaNumeric(31);

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/tenants/{invalidTenantId}");

        // Assert
        await ApiTestHelpers.EnsureErrorStatusCode(response,
            HttpStatusCode.BadRequest,
            $"""Failed to bind parameter "TenantId Id" from "{invalidTenantId}"."""
        );
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var command = new UpdateTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/tenants/{existingTenantId}", command);

        // Assert
        ApiTestHelpers.EnsureSuccessWithEmptyHeaderAndLocation(response);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "TenantUpdated").Should().Be(1);
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
        await ApiTestHelpers.EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

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
        await ApiTestHelpers.EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();

        // Act
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/tenants/{unknownTenantId}");

        //Assert
        await ApiTestHelpers.EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasUsers_ShouldReturnBadRequest()
    {
        // Act
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/tenants/{existingTenantId}");
        TelemetryEventsCollectorSpy.Reset();

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Id", "All users must be deleted before the tenant can be deleted.")
        };
        await ApiTestHelpers.EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasNoUsers_ShouldDeleteTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var existingUserId = DatabaseSeeder.User1.Id;
        await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/users/{existingUserId}");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/tenants/{existingTenantId}");

        // Assert
        ApiTestHelpers.EnsureSuccessWithEmptyHeaderAndLocation(response);
        Connection.RowExists("Tenants", existingTenantId).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.Name == "TenantDeleted").Should().Be(1);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
