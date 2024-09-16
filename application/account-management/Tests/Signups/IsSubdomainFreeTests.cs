using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class IsSubdomainFreeTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task IsSubdomainFree_WhenTenantDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var subdomain = Faker.Subdomain();

        // Act
        var response = await AnonymousHttpClient
            .GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("true");
    }

    [Fact]
    public async Task IsSubdomainFree_WhenTenantExists_ShouldReturnFalse()
    {
        // Arrange
        var subdomain = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AnonymousHttpClient
            .GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("false");
    }
}
