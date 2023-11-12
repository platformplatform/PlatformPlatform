using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Tenants;

public sealed class TenantEndpointsTests : BaseApiTests<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenantWithValidContract()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await TestHttpClient.GetAsync($"/api/tenants/{existingTenantId}");

        // Assert
        EnsureSuccessGetRequest(response);

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'string', 'pattern': '^[a-z0-9]{3,30}$'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'name': {'type': 'string', 'minLength': 1, 'maxLength': 30},
                    'state': {'type': 'string', 'minLength': 1, 'maxLength':20},
                    'phone': {'type': ['null', 'string'], 'maxLength': 20}
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'name', 'state', 'phone'],
                'additionalProperties': false
            }
            """);

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();

        // Act
        var response = await TestHttpClient.GetAsync($"/api/tenants/{unknownTenantId}");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound,
            $"Tenant with id '{unknownTenantId}' not found.");
    }

    [Fact]
    public async Task GetTenant_WhenTenantInvalidTenantId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidTenantId = Faker.Random.AlphaNumeric(31);

        // Act
        var response = await TestHttpClient.GetAsync($"/api/tenants/{invalidTenantId}");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest,
            $"""Failed to bind parameter "TenantId id" from "{invalidTenantId}".""");
    }

    [Fact]
    public async Task CreateTenant_WhenValid_ShouldCreateTenantAndOwnerUser()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
        var command = new CreateTenantCommand(subdomain, Faker.TenantName(), Faker.PhoneNumber(), email);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        await EnsureSuccessPostRequest(response, $"/api/tenants/{subdomain}");
        Connection.RowExists("Tenants", subdomain);
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }).Should().Be(1);
    }

    [Fact]
    public async Task CreateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidSubdomain = Faker.Random.AlphaNumeric(1);
        var invalidName = Faker.Random.String(31);
        var invalidPhone = Faker.Phone.PhoneNumber("+1 ### ###-INVALID");
        var invalidEmail = Faker.InvalidEmail();

        var command = new CreateTenantCommand(invalidSubdomain, invalidName, invalidPhone, invalidEmail);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "Subdomain must be between 3-30 alphanumeric and lowercase characters."),
            new ErrorDetail("Name", "Name must be between 1 and 30 characters."),
            new ErrorDetail("Phone", "Phone must be in a valid format and no longer than 20 characters."),
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTenant_WhenTenantExists_ShouldReturnBadRequest()
    {
        // Arrange
        var unavailableSubdomain = DatabaseSeeder.Tenant1.Id;
        var command = new CreateTenantCommand(unavailableSubdomain, Faker.TenantName(), null, Faker.Internet.Email());

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "The subdomain is not available.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var command = new UpdateTenantCommand { Name = Faker.TenantName(), Phone = Faker.PhoneNumber() };

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/tenants/{existingTenantId}", command);

        // Assert
        EnsureSuccessWithEmptyHeaderAndLocation(response);
    }

    [Fact]
    public async Task UpdateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var invalidName = Faker.Random.String2(31);
        var invalidPhone = Faker.Phone.PhoneNumber("+1 ### ###-INVALID");
        var command = new UpdateTenantCommand { Name = invalidName, Phone = invalidPhone };

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/tenants/{existingTenantId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 30 characters."),
            new ErrorDetail("Phone", "Phone must be in a valid format and no longer than 20 characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();
        var command = new UpdateTenantCommand { Name = Faker.TenantName(), Phone = Faker.PhoneNumber() };

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/tenants/{unknownTenantId}", command);

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound,
            $"Tenant with id '{unknownTenantId}' not found.");
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();

        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/tenants/{unknownTenantId}");

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound,
            $"Tenant with id '{unknownTenantId}' not found.");
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasUsers_ShouldReturnBadRequest()
    {
        // Act
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var response = await TestHttpClient.DeleteAsync($"/api/tenants/{existingTenantId}");

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Id", "All users must be deleted before the tenant can be deleted.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasNoUsers_ShouldDeleteTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var existingUserId = DatabaseSeeder.User1.Id;
        _ = await TestHttpClient.DeleteAsync($"/api/users/{existingUserId}");

        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/tenants/{existingTenantId}");

        // Assert
        EnsureSuccessWithEmptyHeaderAndLocation(response);
        Connection.RowExists("Tenants", existingTenantId).Should().BeFalse();
    }
}