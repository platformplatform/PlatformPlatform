using System.Net.Http.Json;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class GetCurrentTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetCurrentTenant_WhenTenantExists_ShouldReturnTenantWithValidContract()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/tenants/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'string', 'pattern': '^[A-Z0-9]{19}$'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'name': {'type': 'string', 'minLength': 0, 'maxLength': 30},
                    'state': {'type': 'string', 'minLength': 1, 'maxLength':20},
                    'street': {'type': ['null', 'string']},
                    'city': {'type': ['null', 'string']},
                    'zip': {'type': ['null', 'string']},
                    'addressState': {'type': ['null', 'string']},
                    'country': {'type': ['null', 'string']}
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'name', 'state', 'street', 'city', 'zip', 'addressState', 'country'],
                'additionalProperties': false
            }
            """
        );

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentTenant_WithAddress_ShouldReturnAddressFields()
    {
        // Arrange - Set an address first
        var updateCommand = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName(),
            Street = "123 Main Street",
            City = "New York",
            Zip = "10001",
            State = "NY",
            Country = "USA"
        };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", updateCommand);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/tenants/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var responseBody = await response.Content.ReadAsStringAsync();

        responseBody.Should().Contain("\"street\":\"123 Main Street\"");
        responseBody.Should().Contain("\"city\":\"New York\"");
        responseBody.Should().Contain("\"zip\":\"10001\"");
        responseBody.Should().Contain("\"addressState\":\"NY\"");
        responseBody.Should().Contain("\"country\":\"USA\"");
    }

    [Fact]
    public async Task GetCurrentTenant_WithoutAddress_ShouldReturnNullAddressFields()
    {
        // Arrange - Ensure no address is set
        var updateCommand = new UpdateCurrentTenantCommand
        {
            Name = Faker.TenantName()
            // No address fields set
        };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current", updateCommand);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/tenants/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var responseBody = await response.Content.ReadAsStringAsync();

        responseBody.Should().Contain("\"street\":null");
        responseBody.Should().Contain("\"city\":null");
        responseBody.Should().Contain("\"zip\":null");
        responseBody.Should().Contain("\"addressState\":null");
        responseBody.Should().Contain("\"country\":null");
    }
}
