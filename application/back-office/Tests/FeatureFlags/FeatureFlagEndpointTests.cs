using System.Net;
using System.Net.Http.Json;
using BackOffice.Api.Endpoints;
using BackOffice.Database;
using FluentAssertions;
using Xunit;

namespace BackOffice.Tests.FeatureFlags;

public sealed class FeatureFlagEndpointTests : EndpointBaseTest<BackOfficeDbContext>
{
    [Fact]
    public async Task GetFeatureFlags_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        MockAccountApiHandler.ResponseContent = """{"featureFlags":[]}""";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/internal-api/account/feature-flags");
    }

    [Fact]
    public async Task GetFeatureFlags_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        MockAccountApiHandler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetFeatureFlags_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFlagTenants_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string flagKey = "test-flag";
        MockAccountApiHandler.ResponseContent = """{"tenants":[]}""";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{flagKey}/tenants");
    }

    [Fact]
    public async Task GetFlagTenants_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.GetAsync("/api/back-office/feature-flags/test-flag/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateFlag_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string flagKey = "test-flag";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{flagKey}/activate");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task ActivateFlag_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsync("/api/back-office/feature-flags/test-flag/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateFlag_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string flagKey = "test-flag";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{flagKey}/deactivate");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task DeactivateFlag_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsync("/api/back-office/feature-flags/test-flag/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetTenantOverride_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string flagKey = "test-flag";
        var request = new SetTenantOverrideRequest(123, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{flagKey}/tenant-override");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task SetTenantOverride_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsJsonAsync("/api/back-office/feature-flags/test-flag/tenant-override", new SetTenantOverrideRequest(123, true));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string flagKey = "test-flag";
        var request = new SetRolloutPercentageRequest(50);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsJsonAsync("/api/back-office/feature-flags/test-flag/rollout-percentage", new SetRolloutPercentageRequest(50));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFeatureFlags_WhenAccountApiReturnsError_ShouldForwardStatusCode()
    {
        // Arrange
        MockAccountApiHandler.ResponseStatusCode = HttpStatusCode.InternalServerError;
        MockAccountApiHandler.ResponseContent = """{"error":"Something went wrong"}""";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
