using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Domain;
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
        CountBucketsInRange((int)bucketStart.Value, (int)bucketEnd.Value).Should().Be(50);

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

        var bucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var bucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        bucketStart.Should().NotBeNull();
        bucketEnd.Should().NotBeNull();
        CountBucketsInRange((int)bucketStart.Value, (int)bucketEnd.Value).Should().Be(percentage);
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
        bucketStart.Should().Be(0);
        bucketEnd.Should().Be(100);

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
    public async Task GetFlagTenants_WhenTenantScopedFlag_ShouldReturnAllTenantsWithDefaultSource()
    {
        // Arrange
        var flagKey = "sso";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFlagTenantsResponse>();
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
    public async Task GetFlagTenants_WhenTenantHasOverride_ShouldReturnManualOverrideSource()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var overrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", overrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.TenantId == tenantId);
        tenantResult.IsEnabled.Should().BeTrue();
        tenantResult.Source.Should().Be("manual_override");
    }

    [Fact]
    public async Task GetFlagTenants_WhenFlagHasRollout_ShouldReturnAbRolloutSource()
    {
        // Arrange
        var flagKey = "beta-features";
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("bucket_start", 0),
                ("bucket_end", 100)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be("ab_rollout");
                t.IsEnabled.Should().BeTrue();
            }
        );
    }

    [Fact]
    public async Task GetFlagTenants_WhenNonExistentFlag_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/internal-api/account/feature-flags/non-existent/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFlagTenants_WhenSystemScopedFlag_ShouldReturnBadRequest()
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
        IsInBucketRange(50, 40, 60).Should().BeTrue();
        IsInBucketRange(39, 40, 60).Should().BeFalse();
        IsInBucketRange(61, 40, 60).Should().BeFalse();
        IsInBucketRange(40, 40, 60).Should().BeTrue();
        IsInBucketRange(60, 40, 60).Should().BeTrue();
    }

    [Fact]
    public void BucketRange_WhenWrapAround_ShouldMatchCorrectly()
    {
        // Arrange & Act & Assert (wrap-around within 1-99 range)
        IsInBucketRange(95, 90, 10).Should().BeTrue();
        IsInBucketRange(5, 90, 10).Should().BeTrue();
        IsInBucketRange(50, 90, 10).Should().BeFalse();
        IsInBucketRange(90, 90, 10).Should().BeTrue();
        IsInBucketRange(10, 90, 10).Should().BeTrue();
        IsInBucketRange(11, 90, 10).Should().BeFalse();
        IsInBucketRange(89, 90, 10).Should().BeFalse();
    }

    [Fact]
    public void BucketRange_WhenBucketZero_ShouldAlwaysBeIncluded()
    {
        // Bucket 0 = always opt-in, included in any rollout range
        IsInBucketRange(0, 1, 50).Should().BeTrue();
        IsInBucketRange(0, 50, 99).Should().BeTrue();
        IsInBucketRange(0, 90, 10).Should().BeTrue();
        IsInBucketRange(0, 0, 100).Should().BeTrue();
    }

    [Fact]
    public void BucketRange_WhenBucketHundred_ShouldOnlyBeIncludedAtFullRollout()
    {
        // Bucket 100 = always opt-out, only included when range covers all (0-100 = 100% rollout)
        IsInBucketRange(100, 0, 100).Should().BeTrue();
        IsInBucketRange(100, 1, 99).Should().BeFalse();
        IsInBucketRange(100, 1, 50).Should().BeFalse();
        IsInBucketRange(100, 90, 10).Should().BeFalse();
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
        bucket1.Should().BeInRange(1, 99);
    }

    // JWT invalidation tests

    [Fact]
    public async Task ActivateFeatureFlag_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "custom-branding";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetUserFeatureFlag_WhenCalled_ShouldIncrementTenantFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "compact-view";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task SetFeatureFlagRolloutPercentage_WhenCalled_ShouldIncrementAllTenantsFeatureFlagVersion()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id.Value;
        var originalVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedVersion = Connection.ExecuteScalar<long>(
            "SELECT feature_flag_version FROM tenants WHERE id = @tenantId", [new { tenantId }]
        );
        updatedVersion.Should().Be(originalVersion + 1);
    }

    private static bool IsInBucketRange(int bucket, int bucketStart, int bucketEnd)
    {
        if (bucket == 0) return true;
        if (bucket == 100) return bucketStart == 0 && bucketEnd == 100;

        if (bucketStart <= bucketEnd)
        {
            return bucket >= bucketStart && bucket <= bucketEnd;
        }

        return bucket >= bucketStart || bucket <= bucketEnd;
    }

    private static int CountBucketsInRange(int bucketStart, int bucketEnd)
    {
        // For normal rollout (1-99 range), count only the normal buckets
        if (bucketStart <= bucketEnd)
        {
            return bucketEnd - bucketStart + 1;
        }

        return 99 - bucketStart + 1 + bucketEnd;
    }
}
