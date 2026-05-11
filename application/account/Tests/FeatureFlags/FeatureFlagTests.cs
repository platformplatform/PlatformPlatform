using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;
using FeatureFlagRegistry = SharedKernel.FeatureFlags.FeatureFlags;
using FeatureFlagScope = SharedKernel.FeatureFlags.FeatureFlagScope;

namespace Account.Tests.FeatureFlags;

public sealed class FeatureFlagTests : EndpointBaseTest<AccountDbContext>
{
    // Activation tests

    [Fact]
    public async Task ActivateFeatureFlag_WhenValid_ShouldSetEnabledAt()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        enabledAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagActivated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateFeatureFlag_WhenAlreadyActive_ShouldUpdateEnabledAt()
    {
        // Arrange
        var flagKey = "beta-features";
        var originalEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
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
        var flagKey = "beta-features";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        disabledAt.Should().NotBeNullOrEmpty();
        enabledAt.Should().NotBeNullOrEmpty("EnabledAt should be preserved on deactivation");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagDeactivated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenAlreadyInactive_ShouldHandleGracefully()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null);

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
        var flagKey = "beta-features";
        await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1);
        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = false };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1);
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Tenant override tests (owner API)

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForConfigurableFlag_ShouldSucceed()
    {
        // Arrange
        var flagKey = "custom-branding";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

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
        var flagKey = "sso";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{flagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForAdminOnlyFlag_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{flagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "custom-branding";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

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
        var flagKey = "compact-view";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

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
        var flagKey = "sso";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

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
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
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
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId }]
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
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenNoSearchProvided_ShouldReturnAllUsersPaginated()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
        result.PageSize.Should().Be(25);
        result.CurrentPageOffset.Should().Be(0);
        result.TotalPages.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSearchMatchesEmail_ShouldReturnMatchingUsersWithDefaultSource()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.Users.Should().OnlyContain(u => u.Source == "default");
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenUserHasOverride_ShouldReturnUserWithManualOverrideSource()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        var userResult = result.Users.Single(u => u.Id.Value == userId);
        userResult.IsEnabled.Should().BeTrue();
        userResult.Source.Should().Be("manual_override");
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
        var flagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        rolloutBucketStart.Should().NotBeNull();
        rolloutBucketEnd.Should().NotBeNull();
        CountBucketsInRange((int)rolloutBucketStart.Value, (int)rolloutBucketEnd.Value).Should().Be(50);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.rollout_percentage"].Should().Be("50");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task SetFeatureFlagRolloutPercentage_WhenSetToNPercent_ShouldIncludeExactlyNBuckets(int percentage)
    {
        // Arrange
        var flagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = percentage };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        rolloutBucketStart.Should().NotBeNull();
        rolloutBucketEnd.Should().NotBeNull();
        CountBucketsInRange((int)rolloutBucketStart.Value, (int)rolloutBucketEnd.Value).Should().Be(percentage);
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenInvalidPercentage_ShouldFailValidation()
    {
        // Arrange
        var flagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 101 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenNonAbTestEligible_ShouldFailValidation()
    {
        // Arrange
        var flagKey = "sso";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenZeroPercent_ShouldClearBucketRange()
    {
        // Arrange
        var flagKey = "beta-features";
        var setCommand = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", setCommand);
        TelemetryEventsCollectorSpy.Reset();
        var clearCommand = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 0 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", clearCommand);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
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
        var flagKey = "beta-features";
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 100 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        rolloutBucketStart.Should().Be(0);
        rolloutBucketEnd.Should().Be(99);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    // Tenant configurable flags query tests

    [Fact]
    public async Task GetTenantConfigurableFlags_WhenCalled_ShouldReturnConfigurableFlagsWithCurrentOverrideState()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/feature-flags/tenant-configurable");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<TenantConfigurableFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().Contain(f => f.FlagKey == "custom-branding" && f.Enabled == false);
    }

    [Fact]
    public async Task GetUserConfigurableFlags_WhenCalled_ShouldReturnConfigurableUserFlagsWithCurrentOverrideState()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/feature-flags/user-configurable");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<UserConfigurableFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().Contain(f => f.FlagKey == "compact-view" && f.Enabled == false);
    }

    // Flag tenants query tests

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantScopedFlag_ShouldReturnAllTenantsWithDefaultSource()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be("default");
                t.IsEnabled.Should().BeFalse();
            }
        );
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantHasOverride_ShouldReturnManualOverrideSource()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.Id.Value == tenantId);
        tenantResult.IsEnabled.Should().BeTrue();
        tenantResult.Source.Should().Be("manual_override");
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenFlagHasRollout_ShouldReturnAbRolloutSource()
    {
        // Arrange
        var flagKey = "beta-features";
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("bucket_start", 0),
                ("bucket_end", 99)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be("ab_rollout");
                t.IsEnabled.Should().BeTrue();
            }
        );
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantDisabledViaOverrideWhileAbRolloutActive_ShouldReturnManualOverrideDisabled()
    {
        // Arrange - set up A/B rollout at 100% so all tenants are enabled
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("bucket_start", 0),
                ("bucket_end", 99)
            ]
        );

        // Create a disabled override for the tenant (simulating admin toggling OFF)
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", null),
                ("disabled_at", TimeProvider.GetUtcNow()),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert - manual override should take precedence over A/B rollout
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.Id.Value == tenantId);
        tenantResult.IsEnabled.Should().BeFalse();
        tenantResult.Source.Should().Be("manual_override");
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

    // Pagination + filtering tests

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSearchMatchesOwnerEmail_ShouldReturnMatchingTenants()
    {
        // Arrange
        var flagKey = "sso";

        // Act - DatabaseSeeder.Tenant1Owner email is "owner@tenant-1.com", so search by "tenant-1" hits owner email
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?Search=tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
        result.Tenants.Should().OnlyContain(t => t.Owner!.Email.Contains("tenant-1"));
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSearchHasNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?Search=does-not-exist-anywhere");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenPlansFilterMatches_ShouldReturnOnlyTenantsOnSelectedPlans()
    {
        // Arrange - DatabaseSeeder.Tenant1 is on the Basis plan
        var flagKey = "sso";

        // Act
        var matchResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?Plans=Basis");
        var noMatchResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?Plans=Premium");

        // Assert
        matchResponse.ShouldBeSuccessfulGetRequest();
        noMatchResponse.ShouldBeSuccessfulGetRequest();
        var matchResult = await matchResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        var noMatchResult = await noMatchResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        matchResult.Should().NotBeNull();
        noMatchResult.Should().NotBeNull();
        matchResult.Tenants.Should().OnlyContain(t => t.Plan == SubscriptionPlan.Basis);
        matchResult.TotalCount.Should().BeGreaterThan(0);
        noMatchResult.Tenants.Should().BeEmpty();
        noMatchResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenStateEnabledAndTenantHasManualOverride_ShouldReturnOnlyEnabledTenants()
    {
        // Arrange - tenant-1 gets a manual enable for `sso`; all other tenants remain disabled by default
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?State=Enabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.IsEnabled);
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenStateDisabledAndTenantIsEnabledViaOverride_ShouldExcludeTenant()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?State=Disabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotContain(t => t.Id.Value == tenantId);
        result.Tenants.Where(t => t.IsEnabled).Should().BeEmpty();
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenStateOmitted_ShouldNotFilterByState()
    {
        // Arrange - tenant-1 gets enabled, then ask without State (omitted = no filter)
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);

        // Act
        var omittedResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");
        var enabledResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?State=Enabled");

        // Assert
        omittedResponse.ShouldBeSuccessfulGetRequest();
        enabledResponse.ShouldBeSuccessfulGetRequest();
        var omitted = await omittedResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        var enabled = await enabledResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        omitted.Should().NotBeNull();
        enabled.Should().NotBeNull();
        omitted.TotalCount.Should().BeGreaterThanOrEqualTo(enabled.TotalCount);
        omitted.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenPaginated_ShouldReturnCorrectPagingMetadata()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?PageSize=1&PageOffset=0");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.PageSize.Should().Be(1);
        result.CurrentPageOffset.Should().Be(0);
        result.Tenants.Length.Should().BeLessOrEqualTo(1);
        result.TotalPages.Should().Be(result.TotalCount);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSortedByName_ShouldReturnTenantsInAscendingNameOrder()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().BeInAscendingOrder(t => t.Name);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSearchMatchesEmail_ShouldReturnOnlyMatchingUsers()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?Search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.Users.Should().OnlyContain(u => u.Email.Contains("owner@tenant-1"));
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenRolesFilterApplied_ShouldReturnOnlyUsersWithSelectedRoles()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?Roles=Owner");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.Users.Should().OnlyContain(u => u.Role == UserRole.Owner);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenStateEnabledAndUserHasManualOverride_ShouldReturnOnlyEnabledUsers()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertUserOverride(flagKey, tenantId, userId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?State=Enabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().OnlyContain(u => u.IsEnabled);
        result.Users.Should().Contain(u => u.Id.Value == userId);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenStateDisabled_ShouldReturnOnlyDisabledUsers()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertUserOverride(flagKey, tenantId, userId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?State=Disabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().OnlyContain(u => !u.IsEnabled);
        result.Users.Should().NotContain(u => u.Id.Value == userId);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenPaginated_ShouldReturnCorrectPagingMetadata()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?PageSize=1&PageOffset=0");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.PageSize.Should().Be(1);
        result.CurrentPageOffset.Should().Be(0);
        result.Users.Length.Should().BeLessOrEqualTo(1);
        result.TotalPages.Should().Be(result.TotalCount);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenPageOffsetExceedsTotalPages_ShouldReturnBadRequest()
    {
        // Arrange
        var flagKey = "compact-view";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?PageOffset=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // HasOverride filter tests

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideOmitted_ShouldNotFilterByOverride()
    {
        // Arrange - seed an override so the dataset contains both override and non-override rows (Source: "manual_override" + "default")
        var flagKey = "sso";
        InsertTenantOverride(flagKey, DatabaseSeeder.Tenant1.Id, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().Contain(t => t.Source == "manual_override");
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrue_ShouldReturnOnlyTenantsWithManualOverride()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == "manual_override");
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueAndStateDisabled_ShouldReturnDisabledOverrides()
    {
        // Arrange - tenant-1 has a disabling manual override; no other tenants have manual overrides
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, false);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?HasOverride=true&State=Disabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == "manual_override" && !t.IsEnabled);
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueAndNoTenantHasOverride_ShouldReturnEmpty()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueOnAbRolloutFlag_ShouldExcludeAbRolloutAndDefaultRows()
    {
        // Arrange - beta-features is A/B-eligible. Configure a 100% rollout so every tenant evaluates as "ab_rollout",
        // then add a manual override for tenant-1. HasOverride=true should keep only tenant-1.
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("bucket_start", 0),
                ("bucket_end", 99)
            ]
        );
        InsertTenantOverride(flagKey, tenantId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == "manual_override");
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideOmitted_ShouldNotFilterByOverride()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().Contain(u => u.Source == "manual_override");
        result.Users.Should().Contain(u => u.Source == "default");
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideTrue_ShouldReturnOnlyUsersWithManualOverride()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().OnlyContain(u => u.Source == "manual_override");
        result.Users.Should().Contain(u => u.Id.Value == userId);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideTrueAndRoleFiltered_ShouldReturnOnlyMatchingUsersWithOverride()
    {
        // Arrange - only the owner gets the override; member gets none
        var flagKey = "compact-view";
        var ownerId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, ownerId, true);

        // Act
        var matchResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?HasOverride=true&Roles=Owner");
        var noMatchResponse = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/users?HasOverride=true&Roles=Member");

        // Assert
        matchResponse.ShouldBeSuccessfulGetRequest();
        noMatchResponse.ShouldBeSuccessfulGetRequest();
        var matchResult = await matchResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        var noMatchResult = await noMatchResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        matchResult.Should().NotBeNull();
        noMatchResult.Should().NotBeNull();
        matchResult.Users.Should().OnlyContain(u => u.Source == "manual_override" && u.Role == UserRole.Owner);
        matchResult.Users.Should().Contain(u => u.Id.Value == ownerId);
        noMatchResult.Users.Should().BeEmpty();
    }

    private void InsertTenantOverride(string flagKey, TenantId tenantId, bool enabled)
    {
        var overrideId = FeatureFlagId.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", now),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", enabled ? now : null),
                ("disabled_at", enabled ? null : now),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );
    }

    private void InsertUserOverride(string flagKey, TenantId tenantId, string userId, bool enabled)
    {
        var overrideId = FeatureFlagId.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", now),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", userId),
                ("enabled_at", enabled ? now : null),
                ("disabled_at", enabled ? null : now),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );
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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null);

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
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null);

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
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

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
        var flagKey = "custom-branding";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

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
        var flagKey = "compact-view";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

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
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId = tenantId.Value }]
        );
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

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
