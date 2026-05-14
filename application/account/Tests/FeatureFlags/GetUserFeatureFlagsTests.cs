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

public sealed class GetUserFeatureFlagsTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetUserFeatureFlags_WhenCalled_ShouldReturnAllUserScopedFlags()
    {
        // Arrange
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{userId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUserFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        payload.Flags.Should().Contain(f => f.FlagKey == "compact-view");
        payload.Flags.Should().Contain(f => f.FlagKey == "experimental-ui");
    }

    [Fact]
    public async Task GetUserFeatureFlags_WhenUserHasManualOverride_ShouldReturnManualOverrideSource()
    {
        // Arrange
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "compact-view";
        Connection.Insert("feature_flags", [
                ("id", FeatureFlagId.NewId().ToString()),
                ("created_at", TimeProvider.System.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId.Value),
                ("enabled_at", TimeProvider.System.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("source", "Manual"),
                ("scope", "User")
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{userId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUserFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var compactView = payload.Flags.Single(f => f.FlagKey == "compact-view");
        compactView.IsEnabled.Should().BeTrue();
        compactView.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetUserFeatureFlags_WhenBaseRowInactiveAndOverrideActive_ShouldReportIsEnabledFalse()
    {
        // Arrange — globally Deactivate compact-view so the base row reads IsActive=false, but keep an
        // active manual override row for the user. The runtime FeatureFlagEvaluator short-circuits on
        // !baseRow.IsActive (FeatureFlagEvaluator.cs:48) so this query must mirror that contract.
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var flagKey = "compact-view";
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", null)]);
        Connection.Insert("feature_flags", [
                ("id", FeatureFlagId.NewId().ToString()),
                ("created_at", TimeProvider.System.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId.Value),
                ("enabled_at", TimeProvider.System.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("source", "Manual"),
                ("scope", "User")
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{userId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUserFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var compactView = payload.Flags.Single(f => f.FlagKey == "compact-view");
        compactView.IsEnabled.Should().BeFalse();
        compactView.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetUserFeatureFlags_WhenUserInAbRolloutRange_ShouldReturnAbRolloutSourceEnabled()
    {
        // Arrange
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var flagKey = "experimental-ui";
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
        var response = await client.GetAsync($"/api/back-office/users/{userId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUserFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        var experimentalUi = payload.Flags.Single(f => f.FlagKey == "experimental-ui");
        experimentalUi.IsEnabled.Should().BeTrue();
        experimentalUi.Source.Should().Be(FeatureFlagSource.AbRollout);
    }

    [Fact]
    public async Task GetUserFeatureFlags_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = UserId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{nonExistentId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserFeatureFlags_WhenScopeFilteredToUser_ShouldOnlyIncludeUserScopedFlags()
    {
        // Arrange
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{userId}/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUserFeatureFlagsResponse>();
        payload.Should().NotBeNull();
        // No system or tenant scoped flags should leak through
        payload.Flags.Should().NotContain(f => f.FlagKey == "google-oauth");
        payload.Flags.Should().NotContain(f => f.FlagKey == "sso");
        payload.Flags.Should().NotContain(f => f.FlagKey == "beta-features");
        payload.Flags.Should().NotContain(f => f.FlagKey == "account-overview");
    }
}
