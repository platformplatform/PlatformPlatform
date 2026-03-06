using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Tenants.Commands;
using Account.Features.Tenants.Shared;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class UpdateCurrentTenantTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateCurrentTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantResponse = await response.Content.ReadFromJsonAsync<TenantResponse>();
        tenantResponse.Should().NotBeNull();
        tenantResponse.Name.Should().Be(command.Name);

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
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("name", "Name must be between 1 and 30 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCurrentTenant_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand { Name = Faker.TenantName() };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account/tenants/current", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to update tenant information.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
