using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;

namespace PlatformPlatform.AccountManagement.Tests.WebApi.Endpoints;

public class TenantEndpointsTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public TenantEndpointsTests()
    {
        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration.
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                services.Remove(descriptor);

                // Add ApplicationDbContext using an in-memory database for testing.
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("InMemoryDbForTesting"));

                services.AddTransient<DatabaseSeeder>();
            });
        });

        _serviceProvider = _webApplicationFactory.Services;
    }

    [Fact]
    public async Task CreateTenant_WhenValid_ShouldCreateTenant()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/tenants", new CreateTenantRequest("TestTenant"));

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantId = await response.Content.ReadFromJsonAsync<TenantId>();
        tenantId.Should().BeGreaterThan(startId, "We expect a valid Tenant Id greater than the start Id");

        var expectedBody = $@"{{""value"":{tenantId!.AsRawString()}}}";
        var responseAsRawString = await response.Content.ReadAsStringAsync();
        responseAsRawString.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location!.ToString().Should().Be($"/tenants/{tenantId.AsRawString()}");
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
        var expectedBody = @"{""id"":""TenantID"",""name"":""TenantName""}"
            .Replace("TenantID", DatabaseSeeder.Tenant1Id.AsRawString())
            .Replace("TenantName", DatabaseSeeder.Tenant1Name);

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
}