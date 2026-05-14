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

public sealed class GetTenantFeatureFlagsTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
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
        ActivateBaseRow(flagKey);
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
        ActivateBaseRow(flagKey);
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
    public async Task GetTenantFeatureFlags_WhenBaseRowInactiveAndOverrideActive_ShouldReportIsEnabledFalse()
    {
        // Arrange — globally Deactivate beta-features so the base row reads IsActive=false, but keep an
        // active manual override row for the tenant. The runtime FeatureFlagEvaluator short-circuits on
        // !baseRow.IsActive (FeatureFlagEvaluator.cs:48) so this query must mirror that contract.
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "beta-features";
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", null)]);
        InsertTenantOverride(tenantId, flagKey, "Manual", TimeProvider.System.GetUtcNow());
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/feature-flags");

        // Assert — the row Source stays Manual (the override is authoritative for that column), but
        // IsEnabled must be false because the base row is globally deactivated.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTenantFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var betaFeatures = payload.Flags.Single(f => f.FlagKey == "beta-features");
        betaFeatures.IsEnabled.Should().BeFalse();
        betaFeatures.Source.Should().Be(FeatureFlagSource.Manual);
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

    // The reconciler creates non-kill-switch base rows with EnabledAt=null until an admin Activates.
    // Tests that exercise Source attribution over an existing override need the base row Active so
    // the mirror query path is reached at all — FeatureFlagEvaluator.cs:48 short-circuits otherwise.
    private void ActivateBaseRow(string flagKey)
    {
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", TimeProvider.System.GetUtcNow())]);
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
