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
        var command = new UpdateTenantContactInfoCommand(Faker.PhoneNumber(), Faker.Address.StreetAddress(), Faker.Address.City(), Faker.Address.ZipCode(), Faker.Address.CountryCode());

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
        var command = new UpdateTenantContactInfoCommand(Faker.PhoneNumber(), Faker.Address.StreetAddress(), Faker.Address.City(), Faker.Address.ZipCode(), Faker.Address.CountryCode());

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
        var command = new UpdateTenantContactInfoCommand("not-a-phone", Faker.Address.StreetAddress(), Faker.Address.City(), Faker.Address.ZipCode(), Faker.Address.CountryCode());

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
    public async Task UpdateTenantContactInfo_WhenFieldsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTenantContactInfoCommand("+" + Faker.Random.String2(20, "0123456789"), Faker.Random.String2(201), Faker.Random.String2(101), Faker.Random.String2(11), Faker.Random.String2(3));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/tenants/current/contact-info", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("PhoneNumber", "Phone number must be in international format and no more than 20 characters."),
            new ErrorDetail("Street", "Street must be no more than 200 characters."),
            new ErrorDetail("City", "City must be no more than 100 characters."),
            new ErrorDetail("PostalCode", "Postal code must be no more than 10 characters."),
            new ErrorDetail("Country", "Country must be exactly 2 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
