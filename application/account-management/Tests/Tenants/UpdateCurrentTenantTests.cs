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
    public async Task UpdateCurrentTenant_WithCompleteAddress_ShouldUpdateTenantAndTrackAddressUpdated()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = "123 Main Street",
            City = "New York",
            Zip = "10001",
            State = "NY",
            Country = "USA"
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("TenantAddressUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCurrentTenant_WithPartialAddress_ShouldUpdateTenantAndTrackAddressUpdated()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = "123 Main Street",
            City = "New York"
            // Zip, State, Country are null
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("TenantAddressUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCurrentTenant_ClearingAddress_ShouldUpdateTenantAndTrackAddressUpdated()
    {
        // Arrange - First set an address
        var setAddressCommand = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = "123 Main Street",
            City = "New York",
            Zip = "10001",
            State = "NY",
            Country = "USA"
        };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", setAddressCommand);
        TelemetryEventsCollectorSpy.Reset();

        // Act - Clear the address
        var clearAddressCommand = new UpdateCurrentTenantCommand
        {
            Name = setAddressCommand.Name
            // All address fields are null
        };
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", clearAddressCommand);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("TenantAddressUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCurrentTenant_WithSameAddress_ShouldOnlyTrackTenantUpdated()
    {
        // Arrange - First set an address
        var initialCommand = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = "123 Main Street",
            City = "New York",
            Zip = "10001",
            State = "NY",
            Country = "USA"
        };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", initialCommand);
        TelemetryEventsCollectorSpy.Reset();

        // Act - Update with same address but different name
        var sameAddressCommand = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(), // Different name
            Street = initialCommand.Street,
            City = initialCommand.City,
            Zip = initialCommand.Zip,
            State = initialCommand.State,
            Country = initialCommand.Country
        };
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", sameAddressCommand);

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

    [Fact]
    public async Task UpdateCurrentTenant_WithInvalidAddressFields_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = Faker.Random.String2(101), // Too long
            City = Faker.Random.String2(51), // Too long
            Zip = Faker.Random.String2(21), // Too long
            State = Faker.Random.String2(51), // Too long
            Country = Faker.Random.String2(51) // Too long
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Street", "Street must be 100 characters or less."),
            new ErrorDetail("City", "City must be 50 characters or less."),
            new ErrorDetail("Zip", "Zip must be 20 characters or less."),
            new ErrorDetail("State", "State must be 50 characters or less."),
            new ErrorDetail("Country", "Country must be 50 characters or less.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
