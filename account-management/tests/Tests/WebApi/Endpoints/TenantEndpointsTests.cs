using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.WebApi.Endpoints;

public class TenantEndpointsTests
{
    // see https://stackoverflow.com/a/17349663
    private const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

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
}