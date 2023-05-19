using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Tests.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi.Endpoints;
using PlatformPlatform.Foundation.DddCqrsFramework.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.WebApi.Endpoints;

public sealed class TenantEndpointsTests : IDisposable
{
    // This string represents a custom DateTime format based on the built-in format "o".
    // The format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK" is used to avoid trailing zeros in the DateTime string.
    // The 'F's in the format are upper-case to indicate that trailing zeros should be removed.
    // See https://stackoverflow.com/a/17349663
    private const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

    private readonly IServiceProvider _serviceProvider;
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

        _serviceProvider = _webApplicationFactory.Services;
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
        var response = await httpClient.PostAsJsonAsync("/tenants",
            new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantDto>();
        var tenantId = TenantId.FromString(tenantDto!.Id);
        tenantId.Should().BeGreaterThan(startId, "We expect a valid Tenant Id greater than the start Id");

        var tenantName = tenantDto.Name;
        var createdAt = tenantDto.CreatedAt.ToString(Iso8601TimeFormat);
        var expectedBody =
            $@"{{""id"":""{tenantDto.Id}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""name"":""{tenantName}"",""state"":0,""email"":""foo@tenant1.com"",""phone"":""1234567890""}}";

        var responseAsRawString = await response.Content.ReadAsStringAsync();
        responseAsRawString.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location!.ToString().Should().Be($"/tenants/{tenantId.AsRawString()}");
    }

    [Fact]
    public async Task CreateTenant_WhenInValid_ShouldNotCreateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/tenants",
            new CreateTenantCommand("TestTenant", "a", "ab", null)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errors = await response.Content.ReadFromJsonAsync<PropertyError[]>();
        errors!.Length.Should().BeGreaterThan(0);
        errors.Should().Contain(new PropertyError("Subdomain",
            "Subdomains should be 3 to 30 lowercase alphanumeric characters."));
        errors.Should().Contain(new PropertyError("Email",
            "Email must be a valid email address and not exceed 100 characters."));

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenant()
    {
        // Arrange
        using (var serviceScope = _serviceProvider.CreateScope())
        {
            var databaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            databaseSeeder.Seed();
        }

        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.GetAsync($"/tenants/{DatabaseSeeder.Tenant1Id.AsRawString()}");

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantDto>();
        var tenantId = DatabaseSeeder.Tenant1Id.AsRawString();
        const string tenantName = DatabaseSeeder.Tenant1Name;
        var createdAt = tenantDto?.CreatedAt.ToString(Iso8601TimeFormat);

        var expectedBody =
            $@"{{""id"":""{tenantId}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""name"":""{tenantName}"",""state"":0,""email"":""foo@tenant1.com"",""phone"":""1234567890""}}";

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingTenantId = new TenantId(999);

        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.GetAsync($"/tenants/{nonExistingTenantId.AsRawString()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        using (var serviceScope = _serviceProvider.CreateScope())
        {
            var databaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            databaseSeeder.Seed();
        }

        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id.AsRawString();

        // Act
        var response = await httpClient.PutAsJsonAsync($"/tenants/{tenantId}",
            new UpdateTenantRequest("UpdatedName", "updated@tenant1.com", "0987654321")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantDto>();

        tenantDto!.Name.Should().Be("UpdatedName");
        tenantDto.Email.Should().Be("updated@tenant1.com");
        tenantDto.Phone.Should().Be("0987654321");
    }

    [Fact]
    public async Task UpdateTenant_WhenInValid_ShouldReturnBadRequest()
    {
        // Arrange
        using (var serviceScope = _serviceProvider.CreateScope())
        {
            var databaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            databaseSeeder.Seed();
        }

        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id.AsRawString();

        // Act
        var response = await httpClient.PutAsJsonAsync($"/tenants/{tenantId}",
            new UpdateTenantRequest("Invalid Email", "@tenant1.com", "0987654321")
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.PutAsJsonAsync($"/tenants/{nonExistingTenantId}",
            new UpdateTenantRequest("UpdatedName", "updated@tenant1.com", "0987654321")
        );

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.DeleteAsync($"/tenants/{nonExistingTenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantExists_ShouldDeleteTenant()
    {
        // Arrange
        using (var serviceScope = _serviceProvider.CreateScope())
        {
            var databaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            databaseSeeder.Seed();
        }

        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id.AsRawString();

        // Act
        var response = await httpClient.DeleteAsync($"/tenants/{tenantId}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify that is deleted
        var getResponse = await httpClient.GetAsync($"/tenants/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}