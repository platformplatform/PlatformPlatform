using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Tests.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi.Tenants.Contracts;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.WebApi.Endpoints;

public sealed class TenantEndpointsTests : IDisposable
{
    // This string represents a custom DateTime format based on the built-in format "o".
    // The format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK" is used to avoid trailing zeros in the DateTime string.
    // The 'F's in the format are upper-case to indicate that trailing zeros should be removed.
    // See https://stackoverflow.com/a/17349663
    private const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

    private readonly SqliteInMemoryDbContextFactory<ApplicationDbContext> _sqliteInMemoryDbContextFactory;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public TenantEndpointsTests()
    {
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<ApplicationDbContext>();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration.
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                services.Remove(descriptor);

                // Add ApplicationDbContext using SqLiteDbContextFactory
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateContext());

                // Add DbContextOptions<ApplicationDbContext> to the service collection.
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateOptions());

                services.AddTransient<DatabaseSeeder>();
            });
        });

        var serviceScope = _webApplicationFactory.Services.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task CreateTenant_WhenValid_ShouldCreateTenant()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/tenants/v1",
            new CreateTenantRequest("TestTenant", "foo", "foo@tenant1.com", "1234567890")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        var tenantId = (TenantId) tenantDto!.Id;
        tenantId.Should().BeGreaterThan(startId, "We expect a valid Tenant Id greater than the start Id");

        var tenantName = tenantDto.Name;
        var createdAt = tenantDto.CreatedAt.ToString(Iso8601TimeFormat);
        var expectedBody =
            $@"{{""id"":""{tenantDto.Id}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""name"":""{tenantName}"",""state"":0,""email"":""foo@tenant1.com"",""phone"":""1234567890""}}";

        var responseAsRawString = await response.Content.ReadAsStringAsync();
        responseAsRawString.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location!.ToString().Should().Be($"/api/tenants/v1/{tenantId}");
    }

    [Fact]
    public async Task CreateTenant_WhenInValid_ShouldNotCreateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/tenants/v1",
            new CreateTenantRequest("TestTenant", "a", "ab", null)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var expectedBody =
            """{"type":"BadRequest","title":"Validation Error","status":400,"Errors":[{"attributeName":"Email","message":"'Email' is not a valid email address."},{"attributeName":"Subdomain","message":"'Subdomain' must be between 3 and 30 characters. You entered 1 characters."}]}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.GetAsync($"/api/tenants/v1/{tenantId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        const string tenantName = DatabaseSeeder.Tenant1Name;
        var createdAt = tenantDto?.CreatedAt.ToString(Iso8601TimeFormat);

        var expectedBody =
            $@"{{""id"":""{tenantId}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""name"":""{tenantName}"",""state"":0,""email"":""foo@tenant1.com"",""phone"":""1234567890""}}";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingTenantId = new TenantId(999);

        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.GetAsync($"/api/tenants/v1/{nonExistingTenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var expectedBody =
            """{"type":"NotFound","title":"Validation Error","status":404,"detail":"Tenant with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/v1/{tenantId}",
            new UpdateTenantRequest("UpdatedName", "updated@tenant1.com", "0987654321")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        tenantDto!.Name.Should().Be("UpdatedName");
        tenantDto.Email.Should().Be("updated@tenant1.com");
        tenantDto.Phone.Should().Be("0987654321");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenInValid_ShouldReturnBadRequest()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/v1/{tenantId}",
            new UpdateTenantRequest("Invalid Email", "@tenant1.com", "0987654321")
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/v1/{nonExistingTenantId}",
            new UpdateTenantRequest("UpdatedName", "updated@tenant1.com", "0987654321")
        );

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var expectedBody =
            """{"type":"NotFound","title":"Validation Error","status":404,"detail":"Tenant with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.DeleteAsync($"/api/tenants/v1/{nonExistingTenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // const string expectedBody = $@"{{""message"":""Tenant with id '{nonExistingTenantId}' not found.""}}";
        const string expectedBody =
            $@"{{""type"":""NotFound"",""title"":""Validation Error"",""status"":404,""detail"":""Tenant with id '{nonExistingTenantId}' not found.""}}";

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantExists_ShouldDeleteTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.DeleteAsync($"/api/tenants/v1/{tenantId}");

        // Assert
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();

        // Verify that is deleted
        var getResponse = await httpClient.GetAsync($"/api/tenants/v1/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}