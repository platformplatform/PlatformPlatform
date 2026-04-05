using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Queries;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;
using FeatureFlagRegistry = SharedKernel.Domain.FeatureFlags;
using FeatureFlagScope = SharedKernel.Domain.FeatureFlagScope;

namespace Account.Tests.FeatureFlags;

public sealed class FeatureFlagTests : EndpointBaseTest<AccountDbContext>
{
    // Activation tests

    [Fact]
    public async Task ActivateFeatureFlag_WhenValid_ShouldSetEnabledAt()
    {
        // Arrange
        var featureFlagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        enabledAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagActivated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.feature_flag_key"].Should().Be(featureFlagKey);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateFeatureFlag_WhenAlreadyActive_ShouldUpdateEnabledAt()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var originalEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        updatedEnabledAt.Should().NotBe(originalEnabledAt);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagActivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenActive_ShouldSetDisabledAt()
    {
        // Arrange
        var featureFlagKey = "beta-features";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        disabledAt.Should().NotBeNullOrEmpty();
        enabledAt.Should().NotBeNullOrEmpty("EnabledAt should be preserved on deactivation");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagDeactivated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.feature_flag_key"].Should().Be(featureFlagKey);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenAlreadyInactive_ShouldHandleGracefully()
    {
        // Arrange
        var featureFlagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagDeactivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateFeatureFlag_AfterDeactivation_ShouldReactivateFlag()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        enabledAt.Should().NotBeNullOrEmpty();
        disabledAt.Should().BeNull("DisabledAt should be cleared on reactivation");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagActivated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    // Tenant override tests (internal API)

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenEnabled_ShouldCreateOverrideRow()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { featureFlagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1);
        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { featureFlagKey, tenantId = tenantId.Value }]
        );
        enabledAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenDisabledWithNoExistingOverride_ShouldCreateDisabledOverrideRow()
    {
        // Arrange - tenant has no override row (enabled via A/B rollout or default)
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = false };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { featureFlagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1);
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { featureFlagKey, tenantId = tenantId.Value }]
        );
        disabledAt.Should().BeNull("newly created disabled override should not have disabled_at set when never activated");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideRemoved");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenCalledWithoutAuthContext_ShouldSucceed()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    // Remove tenant override tests (internal API)

    [Fact]
    public async Task RemoveTenantFeatureFlagOverride_WhenOverrideExists_ShouldDeleteRow()
    {
        // Arrange - create an override row first
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var featureFlagId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", featureFlagId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("rollout_bucket_start", null),
                ("rollout_bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { featureFlagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideRemoved");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveTenantFeatureFlagOverride_WhenNoOverrideExists_ShouldReturnNotFound()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Tenant override tests (owner API)

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForConfigurableFlag_ShouldSucceed()
    {
        // Arrange
        var featureFlagKey = "custom-branding";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForNonConfigurableFlag_ShouldReturnForbidden()
    {
        // Arrange
        var featureFlagKey = "sso";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{featureFlagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForAdminOnlyFlag_ShouldReturnForbidden()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{featureFlagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var featureFlagKey = "custom-branding";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to configure tenant feature flags.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    // User override tests

    [Fact]
    public async Task SetUserFeatureFlag_WhenUserConfigurable_ShouldCreateOverrideRow()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagUserOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserFeatureFlag_WhenNotUserScoped_ShouldFailValidation()
    {
        // Arrange
        var featureFlagKey = "sso";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/user-override", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    // User override tests (internal API)

    [Fact]
    public async Task SetUserFeatureFlagInternal_WhenEnabled_ShouldCreateOverrideRow()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND user_id = @userId",
            [new { featureFlagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagUserOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFeatureFlagOverride_WhenOverrideExists_ShouldDeleteRow()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var featureFlagId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", featureFlagId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId.Value),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("rollout_bucket_start", null),
                ("rollout_bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND user_id = @userId",
            [new { featureFlagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagUserOverrideRemoved");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFeatureFlagOverride_WhenNoOverrideExists_ShouldReturnNotFound()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenNoSearchProvided_ShouldReturnEmptyArray()
    {
        // Arrange
        var featureFlagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/users");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSearchMatchesEmail_ShouldReturnMatchingUsersWithDefaultSource()
    {
        // Arrange
        var featureFlagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.Users.Should().OnlyContain(u => u.Source == FeatureFlagOverrideSource.Default);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenUserHasOverride_ShouldReturnUserWithManualOverrideSource()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var featureFlagId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", featureFlagId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId.Value),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("rollout_bucket_start", null),
                ("rollout_bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        var userResult = result.Users.Single(u => u.UserId.Value == userId);
        userResult.IsEnabled.Should().BeTrue();
        userResult.Source.Should().Be(FeatureFlagOverrideSource.ManualOverride);
        userResult.Email.Should().NotBe("Unknown");
        userResult.TenantName.Should().NotBe("Unknown");
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenNonUserScopedFlag_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/feature-flags/sso/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Rollout percentage tests

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenValidPercentage_ShouldUpdateBucketRange()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_start FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_end FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        rolloutBucketStart.Should().NotBeNull();
        rolloutBucketEnd.Should().NotBeNull();
        CountBucketsInRange((int)rolloutBucketStart.Value, (int)rolloutBucketEnd.Value).Should().Be(50);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.feature_flag_key"].Should().Be(featureFlagKey);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.rollout_percentage"].Should().Be("50");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task SetFeatureFlagRolloutPercentage_WhenSetToNPercent_ShouldIncludeExactlyNBuckets(int percentage)
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = percentage };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_start FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_end FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        rolloutBucketStart.Should().NotBeNull();
        rolloutBucketEnd.Should().NotBeNull();
        CountBucketsInRange((int)rolloutBucketStart.Value, (int)rolloutBucketEnd.Value).Should().Be(percentage);
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenInvalidPercentage_ShouldFailValidation()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 101 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenNonAbTestEligible_ShouldFailValidation()
    {
        // Arrange
        var featureFlagKey = "sso";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenZeroPercent_ShouldClearBucketRange()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var setCommand = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", setCommand);
        TelemetryEventsCollectorSpy.Reset();
        var clearCommand = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 0 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", clearCommand);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_start FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_end FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        rolloutBucketStart.Should().BeNull();
        rolloutBucketEnd.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenHundredPercent_ShouldSetFullRange()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 100 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_start FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT rollout_bucket_end FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        rolloutBucketStart.Should().Be(0);
        rolloutBucketEnd.Should().Be(99);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    // Flag tenants query tests

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantScopedFlag_ShouldReturnAllTenantsWithDefaultSource()
    {
        // Arrange
        var featureFlagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be(FeatureFlagOverrideSource.Default);
                t.IsEnabled.Should().BeFalse();
            }
        );
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantHasOverride_ShouldReturnManualOverrideSource()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var featureFlagId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", featureFlagId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("rollout_bucket_start", null),
                ("rollout_bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.TenantId.Value == tenantId);
        tenantResult.IsEnabled.Should().BeTrue();
        tenantResult.Source.Should().Be(FeatureFlagOverrideSource.ManualOverride);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenFlagHasRollout_ShouldReturnAbRolloutSource()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var baseFeatureFlagId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        Connection.Update("feature_flags", "id", baseFeatureFlagId, [
                ("rollout_bucket_start", 0),
                ("rollout_bucket_end", 99)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be(FeatureFlagOverrideSource.AbRollout);
                t.IsEnabled.Should().BeTrue();
            }
        );
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantDisabledViaOverrideWhileAbRolloutActive_ShouldReturnManualOverrideDisabled()
    {
        // Arrange - set up A/B rollout at 100% so all tenants are enabled
        var featureFlagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var baseFeatureFlagId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL", [new { featureFlagKey }]
        );
        Connection.Update("feature_flags", "id", baseFeatureFlagId, [
                ("rollout_bucket_start", 0),
                ("rollout_bucket_end", 99)
            ]
        );

        // Create a disabled override for the tenant (simulating admin toggling OFF)
        var featureFlagId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", featureFlagId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", null),
                ("disabled_at", TimeProvider.GetUtcNow()),
                ("rollout_bucket_start", null),
                ("rollout_bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenants");

        // Assert - manual override should take precedence over A/B rollout
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.TenantId.Value == tenantId);
        tenantResult.IsEnabled.Should().BeFalse();
        tenantResult.Source.Should().Be(FeatureFlagOverrideSource.ManualOverride);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenNonExistentFlag_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/feature-flags/non-existent/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSystemScopedFlag_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/feature-flags/google-oauth/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Query tests

    [Fact]
    public async Task GetFeatureFlags_WhenCalled_ShouldReturnAllFlagsWithDatabaseState()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/feature-flags");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().HaveCount(FeatureFlagRegistry.GetAll().Length);

        var googleOauth = result.Flags.Single(f => f.Key == "google-oauth");
        googleOauth.Scope.Should().Be(FeatureFlagScope.System);
        googleOauth.EnabledAt.Should().BeNull();

        var subscriptions = result.Flags.Single(f => f.Key == "subscriptions");
        subscriptions.Scope.Should().Be(FeatureFlagScope.System);
        subscriptions.EnabledAt.Should().BeNull();

        var betaFeatures = result.Flags.Single(f => f.Key == "beta-features");
        betaFeatures.Scope.Should().Be(FeatureFlagScope.Tenant);
        betaFeatures.IsAbTestEligible.Should().BeTrue();
        betaFeatures.IsActive.Should().BeTrue();
        betaFeatures.EnabledAt.Should().NotBeNull();

        var sso = result.Flags.Single(f => f.Key == "sso");
        sso.IsActive.Should().BeFalse();
        sso.EnabledAt.Should().BeNull();

        var customBranding = result.Flags.Single(f => f.Key == "custom-branding");
        customBranding.ConfigurableByTenant.Should().BeTrue();
        customBranding.IsActive.Should().BeTrue();

        var compactView = result.Flags.Single(f => f.Key == "compact-view");
        compactView.ConfigurableByUser.Should().BeTrue();
        compactView.IsActive.Should().BeTrue();
    }

    // A/B consistency tests

    [Fact]
    public void BucketRange_WhenNormalRange_ShouldMatchCorrectly()
    {
        // Arrange & Act & Assert
        IsInRolloutBucketRange(50, 40, 60).Should().BeTrue();
        IsInRolloutBucketRange(39, 40, 60).Should().BeFalse();
        IsInRolloutBucketRange(61, 40, 60).Should().BeFalse();
        IsInRolloutBucketRange(40, 40, 60).Should().BeTrue();
        IsInRolloutBucketRange(60, 40, 60).Should().BeTrue();
    }

    [Fact]
    public void BucketRange_WhenWrapAround_ShouldMatchCorrectly()
    {
        // Arrange & Act & Assert (wrap-around within 0-99 range)
        IsInRolloutBucketRange(95, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(5, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(50, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(90, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(10, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(11, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(89, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(0, 90, 10).Should().BeTrue();
    }

    [Fact]
    public void RolloutBucket_ShouldBeDeterministic()
    {
        // Arrange
        var sequenceNumber = 42;

        // Act
        var bucket1 = RolloutBucketHasher.ComputeRolloutBucket(sequenceNumber);
        var bucket2 = RolloutBucketHasher.ComputeRolloutBucket(sequenceNumber);

        // Assert
        bucket1.Should().Be(bucket2);
        bucket1.Should().BeInRange(0, 99);
    }

    [Fact]
    public void VanDerCorput_ShouldDistributeEvenly()
    {
        // Arrange
        var bucketCounts = new int[100];

        // Act
        for (var i = 0; i < 1000; i++)
        {
            var bucket = RolloutBucketHasher.ComputeRolloutBucket(i);
            bucket.Should().BeInRange(0, 99);
            bucketCounts[bucket]++;
        }

        // Assert
        foreach (var count in bucketCounts)
        {
            count.Should().BeInRange(9, 11, "van der Corput should distribute within +/-1 of ideal");
        }
    }

    // JWT invalidation tests

    [Fact]
    public async Task ActivateFeatureFlag_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "custom-branding";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetUserFeatureFlag_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "compact-view";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{featureFlagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var featureFlagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    private static bool IsInRolloutBucketRange(int bucket, int rolloutBucketStart, int rolloutBucketEnd)
    {
        return RolloutBucketHasher.IsInRolloutBucketRange(bucket, rolloutBucketStart, rolloutBucketEnd);
    }

    private static int CountBucketsInRange(int rolloutBucketStart, int rolloutBucketEnd)
    {
        if (rolloutBucketStart <= rolloutBucketEnd)
        {
            return rolloutBucketEnd - rolloutBucketStart + 1;
        }

        return 100 - rolloutBucketStart + rolloutBucketEnd + 1;
    }
}
