using System.Net;
using System.Net.Http.Json;
using BackOffice.Api.Endpoints;
using BackOffice.Database;
using FluentAssertions;
using SharedKernel.Domain;
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
    public async Task GetFeatureFlagTenants_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        MockAccountApiHandler.ResponseContent = """{"tenants":[]}""";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/back-office/feature-flags/{featureFlagKey}/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/tenants");
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenExternalUser_ShouldReturnForbidden()
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
        const string featureFlagKey = "test-flag";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/api/back-office/feature-flags/{featureFlagKey}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/activate");
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
        const string featureFlagKey = "test-flag";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/api/back-office/feature-flags/{featureFlagKey}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate");
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
        const string featureFlagKey = "test-flag";
        var request = new SetTenantOverrideRequest(new TenantId(123), true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/back-office/feature-flags/{featureFlagKey}/tenant-override", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task SetTenantOverride_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsJsonAsync("/api/back-office/feature-flags/test-flag/tenant-override", new SetTenantOverrideRequest(new TenantId(123), true));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        var request = new SetRolloutPercentageRequest(50);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/back-office/feature-flags/{featureFlagKey}/rollout-percentage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage");
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
    public async Task RemoveTenantOverride_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        var tenantId = new TenantId(123);

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/back-office/feature-flags/{featureFlagKey}/tenant-override?tenantId={tenantId.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override?tenantId={tenantId.Value}");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task RemoveTenantOverride_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.DeleteAsync("/api/back-office/feature-flags/test-flag/tenant-override?tenantId=123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        MockAccountApiHandler.ResponseContent = """{"users":[]}""";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/back-office/feature-flags/{featureFlagKey}/users?search=test@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/users?search=test%40example.com");
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.GetAsync("/api/back-office/feature-flags/test-flag/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserOverride_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        var request = new SetUserOverrideRequest(UserId.NewId(), new TenantId(123), true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/back-office/feature-flags/{featureFlagKey}/user-override", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/user-override");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task SetUserOverride_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedExternalHttpClient.PutAsJsonAsync("/api/back-office/feature-flags/test-flag/user-override", new SetUserOverrideRequest(UserId.NewId(), new TenantId(123), true));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveUserOverride_WhenInternalUser_ShouldProxyToAccountApi()
    {
        // Arrange
        const string featureFlagKey = "test-flag";
        var userId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/back-office/feature-flags/{featureFlagKey}/user-override?userId={userId}&tenantId=123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MockAccountApiHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal-api/account/feature-flags/{featureFlagKey}/user-override?userId={userId}&tenantId=123");
        MockAccountApiHandler.LastRequest.Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task RemoveUserOverride_WhenExternalUser_ShouldReturnForbidden()
    {
        // Act
        var userId = UserId.NewId();
        var response = await AuthenticatedExternalHttpClient.DeleteAsync($"/api/back-office/feature-flags/test-flag/user-override?userId={userId}&tenantId=123");

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
