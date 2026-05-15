using System.Net;
using System.Net.Http.Json;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class GetTenantActivityTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetTenantActivity_WhenTenantExists_ShouldReturnEmptyEventsList()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/activity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantActivityResponse>();
        payload.Should().NotBeNull();
        payload.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenantActivity_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/activity");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
