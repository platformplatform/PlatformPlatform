using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Addresses.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Addresses;

public sealed class SearchAddressesTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task SearchAddresses_WhenValidQuery_ShouldReturnEmptyResultsWithoutApiKey()
    {
        // Arrange - No API key configured in test configuration, so should return empty results
        var query = "123 Main Street";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/addresses/search?query={Uri.EscapeDataString(query)}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<SearchAddressesResponse>();
        result.Should().NotBeNull();
        result!.Suggestions.Should().BeEmpty(); // Should be empty because no API key is configured
    }

    [Fact]
    public async Task SearchAddresses_WhenEmptyQuery_ShouldReturnEmptyResults()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/addresses/search?query=");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<SearchAddressesResponse>();
        result.Should().NotBeNull();
        result!.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAddresses_WhenNoQuery_ShouldReturnEmptyResults()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/addresses/search");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<SearchAddressesResponse>();
        result.Should().NotBeNull();
        result!.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAddresses_WhenQueryTooLong_ShouldReturnValidationError()
    {
        // Arrange
        var longQuery = new string('a', 201); // Exceeds 200 character limit

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/addresses/search?query={Uri.EscapeDataString(longQuery)}");

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Query", "Search query must be no longer than 200 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task SearchAddresses_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account-management/addresses/search?query=123 Main St");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchAddresses_WhenMemberUser_ShouldReturnEmptyResultsWithoutApiKey()
    {
        // Arrange
        var query = "123 Main Street";

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account-management/addresses/search?query={Uri.EscapeDataString(query)}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<SearchAddressesResponse>();
        result.Should().NotBeNull();
        result!.Suggestions.Should().BeEmpty(); // Should be empty because no API key is configured
    }

    [Fact]
    public async Task SearchAddresses_WhenSpecialCharactersInQuery_ShouldHandleCorrectly()
    {
        // Arrange
        var query = "123 Main St & Co.";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/addresses/search?query={Uri.EscapeDataString(query)}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<SearchAddressesResponse>();
        result.Should().NotBeNull();
        result!.Suggestions.Should().BeEmpty(); // Should be empty because no API key is configured
    }
}
