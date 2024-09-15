using System.Net;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class GetTenantTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenantWithValidContract()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/tenants/{existingTenantId}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

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
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");
    }

    [Fact]
    public async Task GetTenant_WhenTenantInvalidTenantId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidTenantId = Faker.Random.AlphaNumeric(31);

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/tenants/{invalidTenantId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"""Failed to bind parameter "TenantId id" from "{invalidTenantId}".""");
    }
}
