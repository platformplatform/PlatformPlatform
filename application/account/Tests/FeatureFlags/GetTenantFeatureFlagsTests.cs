using System.Net;
using System.Net.Http.Json;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Queries;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.FeatureFlags;

public sealed class GetTenantFeatureFlagsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenantFeatureFlags_WhenCalled_ShouldReturnAllTenantScopedFlagsWithDefaultSource()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        payload.Flags.Should().NotBeEmpty();
        payload.Flags.Should().Contain(f => f.FlagKey == "beta-features");
        payload.Flags.Should().Contain(f => f.FlagKey == "sso");
        payload.Flags.Should().Contain(f => f.FlagKey == "account-overview");
    }

    [Fact]
    public async Task GetTenantFeatureFlags_WhenTenantHasManualOverrideOnPlanGatedFlag_ShouldReturnManualOverrideSource()
    {
        // Arrange — plan-gated flag (sso) but the row Source column is "Manual" (admin manually toggled it on).
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "sso";
        InsertTenantOverride(tenantId, flagKey, "Manual", TimeProvider.System.GetUtcNow());
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var sso = payload.Flags.Single(f => f.FlagKey == "sso");
        sso.IsEnabled.Should().BeTrue();
        sso.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetTenantFeatureFlags_WhenTenantHasPlanGrantedRow_ShouldReturnPlanSource()
    {
        // Arrange — plan-gated flag (sso) with Source="Plan" (the upgrade evaluator wrote it).
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "sso";
        InsertTenantOverride(tenantId, flagKey, "Plan", TimeProvider.System.GetUtcNow());
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var sso = payload.Flags.Single(f => f.FlagKey == "sso");
        sso.IsEnabled.Should().BeTrue();
        sso.Source.Should().Be(FeatureFlagSource.Plan);
    }

    [Fact]
    public async Task GetTenantFeatureFlags_WhenTenantInAbRolloutRange_ShouldReturnAbRolloutSourceEnabled()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "beta-features";
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("bucket_start", 0),
                ("bucket_end", 99)
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var betaFeatures = payload.Flags.Single(f => f.FlagKey == "beta-features");
        betaFeatures.IsEnabled.Should().BeTrue();
        betaFeatures.Source.Should().Be(FeatureFlagSource.AbRollout);
    }

    [Fact]
    public async Task GetTenantFeatureFlags_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{nonExistentId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTenantFeatureFlags_WhenScopeFilteredToTenant_ShouldOnlyIncludeTenantScopedFlags()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        payload.Flags.Should().NotContain(f => f.FlagKey == "google-oauth");
        payload.Flags.Should().NotContain(f => f.FlagKey == "subscriptions");
        payload.Flags.Should().NotContain(f => f.FlagKey == "compact-view");
        payload.Flags.Should().NotContain(f => f.FlagKey == "experimental-ui");
    }

    private void InsertTenantOverride(TenantId tenantId, string flagKey, string source, DateTimeOffset? enabledAt)
    {
        Connection.Insert("feature_flags", [
                ("id", FeatureFlagId.NewId().ToString()),
                ("created_at", TimeProvider.System.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", enabledAt),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("source", source),
                ("scope", "Tenant")
            ]
        );
    }
}
