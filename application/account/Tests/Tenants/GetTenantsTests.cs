using Account.Database;
using Account.Features.Tenants.Queries;
using FluentAssertions;
using SharedKernel.Tests;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class GetTenantsTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTenants_WhenCalled_ShouldReturnAllTenants()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
        result.Tenants.Should().Contain(t => t.Id == DatabaseSeeder.Tenant1.Id);
    }

    [Fact]
    public async Task GetTenants_WhenCalledWithoutAuth_ShouldSucceed()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/internal-api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
    }
}
