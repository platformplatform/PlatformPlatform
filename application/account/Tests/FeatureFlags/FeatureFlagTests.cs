using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using FluentAssertions;
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
        disabledAt.Should().NotBeNullOrEmpty();

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
        disabledAt.Should().NotBeNullOrEmpty();
        string.Compare(enabledAt, disabledAt, StringComparison.Ordinal).Should().BeGreaterThan(0, "EnabledAt should be after DisabledAt");

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
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId }]
        );
        rowCount.Should().Be(1);
        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId }]
        );
        enabledAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenCalledWithoutAuthContext_ShouldSucceed()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
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

        var bucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var bucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        bucketStart.Should().NotBeNull();
        bucketEnd.Should().NotBeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.rollout_percentage"].Should().Be("50");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
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

        var bucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var bucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        bucketStart.Should().BeNull();
        bucketEnd.Should().BeNull();

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

        var bucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var bucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        bucketStart.Should().Be(1);
        bucketEnd.Should().Be(100);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
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
        IsInBucketRange(50, 40, 60).Should().BeTrue();
        IsInBucketRange(39, 40, 60).Should().BeFalse();
        IsInBucketRange(61, 40, 60).Should().BeFalse();
        IsInBucketRange(40, 40, 60).Should().BeTrue();
        IsInBucketRange(60, 40, 60).Should().BeTrue();
    }

    [Fact]
    public void BucketRange_WhenWrapAround_ShouldMatchCorrectly()
    {
        // Arrange & Act & Assert
        IsInBucketRange(95, 90, 10).Should().BeTrue();
        IsInBucketRange(5, 90, 10).Should().BeTrue();
        IsInBucketRange(50, 90, 10).Should().BeFalse();
        IsInBucketRange(90, 90, 10).Should().BeTrue();
        IsInBucketRange(10, 90, 10).Should().BeTrue();
        IsInBucketRange(11, 90, 10).Should().BeFalse();
        IsInBucketRange(89, 90, 10).Should().BeFalse();
    }

    [Fact]
    public void RolloutBucket_ShouldBeDeterministic()
    {
        // Arrange
        var entityId = "test-entity-123";

        // Act
        var bucket1 = RolloutBucketHasher.ComputeBucket(entityId);
        var bucket2 = RolloutBucketHasher.ComputeBucket(entityId);

        // Assert
        bucket1.Should().Be(bucket2);
        bucket1.Should().BeInRange(1, 100);
    }

    private static bool IsInBucketRange(int bucket, int bucketStart, int bucketEnd)
    {
        if (bucketStart <= bucketEnd)
        {
            return bucket >= bucketStart && bucket <= bucketEnd;
        }

        return bucket >= bucketStart || bucket <= bucketEnd;
    }
}
