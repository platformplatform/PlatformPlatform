using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Tenants.Commands;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class UpdateTenantContactInfoTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateTenantContactInfo_WhenValid_ShouldUpdateContactInfo()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Address.StreetAddress(), Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), Faker.PhoneNumber());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantContactInfoUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenPhoneNumberIsNull_ShouldUpdateContactInfo()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Address.StreetAddress(), Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantContactInfoUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenPhoneNumberIsEmpty_ShouldUpdateContactInfo()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Address.StreetAddress(), Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), "");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantContactInfoUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Address.StreetAddress(), Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), Faker.PhoneNumber());

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to update tenant contact information.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenInvalidPhoneNumber_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Address.StreetAddress(), Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), "not-a-phone");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("PhoneNumber", "Phone number must be in international format and no more than 20 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenAddressHasTooManyLines_ShouldReturnBadRequest()
    {
        // Arrange
        var threeLineAddress = "Line 1\nLine 2\nLine 3";
        var command = new UpdateTenantContactInfoCommand(threeLineAddress, Faker.Address.ZipCode(), Faker.Address.City(), Faker.Address.State(), Faker.Address.CountryCode(), Faker.PhoneNumber());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Address", "Address must be no more than 200 characters and 2 lines.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenantContactInfo_WhenFieldsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand(Faker.Random.String2(201), Faker.Random.String2(11), Faker.Random.String2(101), Faker.Random.String2(51), Faker.Random.String2(3), "+" + Faker.Random.String2(20, "0123456789"));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Address", "Address must be no more than 200 characters and 2 lines."),
            new ErrorDetail("PostalCode", "Postal code must be no more than 10 characters."),
            new ErrorDetail("City", "City must be no more than 100 characters."),
            new ErrorDetail("State", "State must be no more than 50 characters."),
            new ErrorDetail("Country", "Country must be exactly 2 characters."),
            new ErrorDetail("PhoneNumber", "Phone number must be in international format and no more than 20 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
