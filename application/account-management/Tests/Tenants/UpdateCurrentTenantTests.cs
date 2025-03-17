using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class UpdateCurrentTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task UpdateCurrentTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCurrentTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidName = Faker.Random.String2(31);
        var command = new UpdateCurrentTenantCommand { Name = invalidName };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 30 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
