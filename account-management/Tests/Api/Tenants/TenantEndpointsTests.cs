using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Tenants;

public sealed class TenantEndpointsTests : BaseApiTests<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateTenant_WhenValid_ShouldCreateTenant()
    {
        // Act
        var command = new CreateTenant.Command("tenant2", "TestTenant", "1234567890", "test@test.com");
        var response = await TestHttpClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        await EnsureSuccessPostRequest(response, "/api/tenants/tenant2");
    }

    [Fact]
    public async Task CreateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Act
        var command = new CreateTenant.Command("a", "TestTenant", null, "ab");
        var response = await TestHttpClient.PostAsJsonAsync("/api/tenants", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "'Email' is not a valid email address."),
            new ErrorDetail("Subdomain", "'Subdomain' must be between 3 and 30 characters. You entered 1 characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenant()
    {
        // Act
        var response = await TestHttpClient.GetAsync($"/api/tenants/{DatabaseSeeder.Tenant1Id}");

        // Assert
        EnsureSuccessGetRequest(response);

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        const string tenantName = DatabaseSeeder.Tenant1Name;
        var createdAt = tenantDto?.CreatedAt.ToString(Iso8601TimeFormat);
        var expectedBody =
            $$"""{"id":"{{DatabaseSeeder.Tenant1Id}}","createdAt":"{{createdAt}}","modifiedAt":null,"name":"{{tenantName}}","state":0,"phone":"1234567890"}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await TestHttpClient.GetAsync("/api/tenants/unknown");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "Tenant with id 'unknown' not found.");
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Act
        var command = new UpdateTenant.Command {Name = "UpdatedName", Phone = "0987654321"};
        var response = await TestHttpClient.PutAsJsonAsync($"/api/tenants/{DatabaseSeeder.Tenant1Id}", command);

        // Assert
        EnsureSuccessPutRequest(response);
    }

    [Fact]
    public async Task UpdateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Act
        var command = new UpdateTenant.Command {Name = "Invalid phone", Phone = "01-800-HOTLINE"};
        var response = await TestHttpClient.PutAsJsonAsync($"/api/tenants/{DatabaseSeeder.Tenant1Id}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Phone", "'Phone' is not in the correct format.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Act
        var command = new UpdateTenant.Command {Name = "UpdatedName", Phone = "0987654321"};
        var response = await TestHttpClient.PutAsJsonAsync("/api/tenants/unknown", command);

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "Tenant with id 'unknown' not found.");
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await TestHttpClient.DeleteAsync("/api/tenants/unknown");

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "Tenant with id 'unknown' not found.");
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantWithUsersExists_ShouldReturnBadRequest()
    {
        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/tenants/{DatabaseSeeder.Tenant1Id}");

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Id", "All users must be deleted before the tenant can be deleted.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantExistsWithNoUsers_ShouldDeleteTenant()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1Id;
        var _ = await TestHttpClient.DeleteAsync($"/api/users/{DatabaseSeeder.User1Id}");

        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/tenants/{tenantId}");

        // Assert
        EnsureSuccessDeleteRequest(response);

        // Verify that Tenant is deleted
        response = await TestHttpClient.GetAsync($"/api/tenants/{tenantId}");
        var expectedDetail = $"Tenant with id '{tenantId}' not found.";
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, expectedDetail);
    }
}