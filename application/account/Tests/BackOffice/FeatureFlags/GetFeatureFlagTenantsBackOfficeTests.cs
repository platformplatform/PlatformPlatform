using System.Net;
using System.Net.Http.Json;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Queries;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.FeatureFlags;

public sealed class GetFeatureFlagTenantsBackOfficeTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueOnBackOfficeRoute_ShouldReturnOnlyTenantsWithManualOverride()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", now),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", now),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == "manual_override");
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideOmittedOnBackOfficeRoute_ShouldReturnMixedSources()
    {
        // Arrange - one tenant has a manual override, default tenants do not
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", now),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", now),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().Contain(t => t.Source == "manual_override");
    }
}
