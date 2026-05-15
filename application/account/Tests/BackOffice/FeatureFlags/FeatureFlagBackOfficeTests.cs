using System.Net;
using System.Net.Http.Json;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Authentication;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;
using FeatureFlagRegistry = SharedKernel.FeatureFlags.FeatureFlags;
using FeatureFlagScope = SharedKernel.FeatureFlags.FeatureFlagScope;

namespace Account.Tests.BackOffice.FeatureFlags;

// Exercises the back-office feature-flag endpoints at /api/back-office/feature-flags/*.
// All mutations (activate, deactivate, tenant/user overrides, rollout percentage, delete) carry the
// AdminPolicyName requirement; GET queries use the regular back-office identity policy so support
// staff can investigate state without being able to change it.
public sealed class FeatureFlagBackOfficeTests : BackOfficeEndpointBaseTest
{
    private const string RegularBackOfficeIdentityId = "user";
    private const string AdminBackOfficeIdentityId = "admin";

    private HttpClient CreateRegularBackOfficeClient()
    {
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == RegularBackOfficeIdentityId);
        return CreateBackOfficeClientForIdentity(identity);
    }

    private HttpClient CreateAdminBackOfficeClient()
    {
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == AdminBackOfficeIdentityId);
        return CreateBackOfficeClientForIdentity(identity);
    }

    // Activate / deactivate kill-switch (AdminPolicyName)

    [Fact]
    public async Task ActivateFeatureFlag_WhenAdmin_ShouldSetEnabledAt()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        enabledAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagActivated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateFeatureFlag_WhenAlreadyActiveAndAdmin_ShouldUpdateEnabledAt()
    {
        // Arrange
        var flagKey = "beta-features";
        var originalEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        updatedEnabledAt.Should().NotBe(originalEnabledAt);
    }

    [Fact]
    public async Task ActivateFeatureFlag_WhenFlagIsNotKillSwitch_ShouldReturnBadRequest()
    {
        // Arrange — sso is a PlanGatedTenantFlag without IsKillSwitchEnabled set; the validator
        // must reject global Activate so the panic-button contract is enforced.
        var flagKey = "sso";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("FlagKey", "Only kill-switch-enabled feature flags can be globally activated.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenFlagIsNotKillSwitch_ShouldReturnBadRequest()
    {
        // Arrange — sso is a PlanGatedTenantFlag without IsKillSwitchEnabled set; the validator
        // must reject global Deactivate so the panic-button contract is enforced.
        var flagKey = "sso";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("FlagKey", "Only kill-switch-enabled feature flags can be globally deactivated.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ActivateFeatureFlag_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert - the activate route is gated by AdminPolicyName, so a regular back-office identity
        // without the admin group claim must be rejected.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenAdmin_ShouldSetDisabledAt()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

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

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagDeactivated");
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenAlreadyInactiveAndAdmin_ShouldHandleGracefully()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        // First deactivate to make it inactive, then deactivate again to test idempotency
        await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateFeatureFlag_AfterDeactivationByAdmin_ShouldReactivateFlag()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        disabledAt.Should().BeNull("DisabledAt should be cleared on reactivation");
    }

    // Tenant override (AdminPolicy)

    [Fact]
    public async Task SetTenantOverride_WhenEnabledAsAdmin_ShouldCreateOverrideRow()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", command);

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

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task RemoveTenantOverride_WhenOverrideExists_ShouldDeleteRow()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagTenantOverrideRemoved");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task RemoveTenantOverride_WhenNoOverrideExists_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetTenantOverride_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateRegularBackOfficeClient();
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", command);

        // Assert - tenant-override mutations are gated by AdminPolicyName; a regular back-office identity
        // without the admin group claim must be rejected before any repository write.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(0, "the policy guard must run before any repository write");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTenantOverride_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1, "the override must remain when a non-admin caller is rejected");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    // User override (AdminPolicy)

    [Fact]
    public async Task SetUserOverride_WhenEnabledAsAdmin_ShouldCreateOverrideRow()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagUserOverrideSet");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task RemoveUserOverride_WhenOverrideExists_ShouldDeleteRow()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertUserOverride(flagKey, tenantId, userId, true);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId }]
        );
        rowCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagUserOverrideRemoved");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task RemoveUserOverride_WhenNoOverrideExists_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetUserOverride_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateRegularBackOfficeClient();
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(0, "the policy guard must run before any repository write");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveUserOverride_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertUserOverride(flagKey, tenantId, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId }]
        );
        rowCount.Should().Be(1, "the override must remain when a non-admin caller is rejected");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    // User/tenant verification guards — the back-office can target any tenant, so handlers verify the
    // (TenantId, UserId) pair exists before any repository write. A mismatched or non-existent target
    // must short-circuit with NotFound, leaving no override row and no telemetry behind.

    [Fact]
    public async Task SetUserFeatureFlagInternal_WhenUserDoesNotBelongToTenant_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var otherTenantId = new TenantId(999999);
        using var client = CreateAdminBackOfficeClient();
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = otherTenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/user-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User '{userId}' not found in tenant '{otherTenantId}'.");

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(0, "the guard must run before any repository write");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveUserFeatureFlagOverride_WhenUserDoesNotBelongToTenant_ShouldReturnNotFound()
    {
        // Arrange - seed an override on the real tenant; the back-office request targets a different
        // tenant so the guard must reject it without touching the existing override row.
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var realTenantId = DatabaseSeeder.Tenant1.Id;
        var otherTenantId = new TenantId(999999);
        InsertUserOverride(flagKey, realTenantId, userId.Value, true);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/user-override?userId={userId}&tenantId={otherTenantId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User '{userId}' not found in tenant '{otherTenantId}'.");

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(1, "the guard must run before any repository write");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetUserFeatureFlagInternal_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "compact-view";
        var missingUserId = UserId.NewId();
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetUserFeatureFlagInternalCommand { UserId = missingUserId, TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/user-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User '{missingUserId}' not found in tenant '{tenantId}'.");

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = missingUserId.Value }]
        );
        rowCount.Should().Be(0, "the guard must run before any repository write");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "beta-features";
        var missingTenantId = new TenantId(999999);
        using var client = CreateAdminBackOfficeClient();
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = missingTenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant '{missingTenantId}' not found.");

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = missingTenantId.Value }]
        );
        rowCount.Should().Be(0, "the guard must run before any repository write");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetTenantFeatureFlagInternal_WhenDisableAndNoOverrideExists_ShouldCreateExplicitDisableOverride()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = false };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        rowCount.Should().Be(1, "an explicit-disable override must be written so the evaluator overrides the rollout for this tenant");

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            [new { flagKey, tenantId = tenantId.Value }]
        );
        enabledAt.Should().NotBeNullOrEmpty();
        disabledAt.Should().NotBeNullOrEmpty();
        enabledAt.Should().Be(disabledAt, "EnabledAt and DisabledAt at the same instant make FeatureFlag.IsActive evaluate to false");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagTenantOverrideRemoved");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task SetUserFeatureFlagInternal_WhenDisableAndNoOverrideExists_ShouldCreateExplicitDisableOverride()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id;
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetUserFeatureFlagInternalCommand { UserId = userId, TenantId = tenantId, Enabled = false };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        rowCount.Should().Be(1, "an explicit-disable override must be written so the evaluator overrides the rollout for this user");

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = @flagKey AND user_id = @userId",
            [new { flagKey, userId = userId.Value }]
        );
        enabledAt.Should().NotBeNullOrEmpty();
        disabledAt.Should().NotBeNullOrEmpty();
        enabledAt.Should().Be(disabledAt, "EnabledAt and DisabledAt at the same instant make FeatureFlag.IsActive evaluate to false");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagUserOverrideRemoved");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Internal");
    }

    [Fact]
    public async Task RemoveTenantFeatureFlagOverride_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var flagKey = "beta-features";
        var missingTenantId = new TenantId(999999);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override?tenantId={missingTenantId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant '{missingTenantId}' not found.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    // Rollout percentage (AdminPolicy)

    [Fact]
    public async Task SetRolloutPercentage_WhenValidPercentageAsAdmin_ShouldUpdateBucketRange()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

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

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagRolloutPercentageUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_percentage"].Should().Be("0");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_percentage"].Should().Be("50");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task SetRolloutPercentage_WhenSetToNPercent_ShouldIncludeExactlyNBuckets(int percentage)
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = percentage };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var rolloutBucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        var rolloutBucketEnd = Connection.ExecuteScalar<long?>(
            "SELECT bucket_end FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        CountBucketsInRange((int)rolloutBucketStart!.Value, (int)rolloutBucketEnd!.Value).Should().Be(percentage);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenInvalidPercentage_ShouldFailValidation()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 101 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenNonAbTestEligibleFlag_ShouldFailValidation()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenZeroPercent_ShouldClearBucketRange()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 });
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 0 });

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
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateRegularBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var bucketStart = Connection.ExecuteScalar<long?>(
            "SELECT bucket_start FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        bucketStart.Should().BeNull("the policy guard must run before any repository write");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenHundredPercent_ShouldSetFullRange()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 100 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

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
    }

    // Flag tenants query (regular PolicyName)

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantScopedFlag_ShouldReturnAllTenantsWithDefaultSource()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().NotBeEmpty();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be(FeatureFlagSource.Default);
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
        ActivateBaseRow(flagKey);
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.Id.Value == tenantId);
        tenantResult.IsEnabled.Should().BeTrue();
        tenantResult.Source.Should().Be(FeatureFlagSource.Manual);
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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().AllSatisfy(t =>
            {
                t.Source.Should().Be(FeatureFlagSource.AbRollout);
                t.IsEnabled.Should().BeTrue();
            }
        );
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenTenantDisabledViaOverrideWhileAbRolloutActive_ShouldReturnManualOverrideDisabled()
    {
        // Arrange - 100% rollout means every tenant is enabled, then add a disabled manual override.
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
        InsertTenantOverride(flagKey, tenantId, false);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert - manual override takes precedence over the A/B rollout.
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.Id.Value == tenantId);
        tenantResult.IsEnabled.Should().BeFalse();
        tenantResult.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenBaseRowInactiveAndOverrideActive_ShouldReportIsEnabledFalse()
    {
        // Arrange — globally Deactivate beta-features (base row enabled_at=null) but keep an active
        // manual override row for the tenant. The runtime FeatureFlagEvaluator short-circuits on
        // !baseRow.IsActive (FeatureFlagEvaluator.cs:48) so this bulk list must mirror that contract.
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", null)]);
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert — row Source stays Manual (override is authoritative for that column), but
        // IsEnabled must be false because the global kill switch is off.
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        var tenantResult = result.Tenants.Single(t => t.Id.Value == tenantId);
        tenantResult.IsEnabled.Should().BeFalse();
        tenantResult.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenNonExistentFlag_ShouldReturnBadRequest()
    {
        using var client = CreateRegularBackOfficeClient();
        var response = await client.GetAsync("/api/back-office/feature-flags/non-existent/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSystemScopedFlag_ShouldReturnBadRequest()
    {
        using var client = CreateRegularBackOfficeClient();
        var response = await client.GetAsync("/api/back-office/feature-flags/google-oauth/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSearchMatchesOwnerEmail_ShouldReturnMatchingTenants()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act - DatabaseSeeder.Tenant1Owner email is "owner@tenant-1.com", so "tenant-1" hits owner email.
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?Search=tenant-1");

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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?Search=does-not-exist-anywhere");

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
        // Arrange - DatabaseSeeder.Tenant1 is on the Basis plan.
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var matchResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?Plans=Basis");
        var noMatchResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?Plans=Premium");

        // Assert
        matchResponse.ShouldBeSuccessfulGetRequest();
        noMatchResponse.ShouldBeSuccessfulGetRequest();
        var matchResult = await matchResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        var noMatchResult = await noMatchResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        matchResult!.Tenants.Should().OnlyContain(t => t.Plan == SubscriptionPlan.Basis);
        matchResult.TotalCount.Should().BeGreaterThan(0);
        noMatchResult!.Tenants.Should().BeEmpty();
        noMatchResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenStateEnabledAndTenantHasManualOverride_ShouldReturnOnlyEnabledTenants()
    {
        // Arrange - tenant-1 gets a manual enable for `sso`; other tenants remain disabled.
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        ActivateBaseRow(flagKey);
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?State=Enabled");

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
        ActivateBaseRow(flagKey);
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?State=Disabled");

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
        // Arrange - tenant-1 gets enabled, then ask without State.
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var omittedResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");
        var enabledResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?State=Enabled");

        // Assert
        omittedResponse.ShouldBeSuccessfulGetRequest();
        enabledResponse.ShouldBeSuccessfulGetRequest();
        var omitted = await omittedResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        var enabled = await enabledResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        omitted!.TotalCount.Should().BeGreaterThanOrEqualTo(enabled!.TotalCount);
        omitted.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenPaginated_ShouldReturnCorrectPagingMetadata()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?PageSize=1&PageOffset=0");

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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().BeInAscendingOrder(t => t.Name);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideOmitted_ShouldNotFilterByOverride()
    {
        // Arrange - seed an override so the dataset has both override and non-override rows.
        var flagKey = "sso";
        InsertTenantOverride(flagKey, DatabaseSeeder.Tenant1.Id, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().Contain(t => t.Source == FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrue_ShouldReturnOnlyTenantsWithManualOverride()
    {
        // Arrange
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == FeatureFlagSource.Manual);
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueAndStateDisabled_ShouldReturnDisabledOverrides()
    {
        // Arrange - tenant-1 has a disabling manual override.
        var flagKey = "sso";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        InsertTenantOverride(flagKey, tenantId, false);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?HasOverride=true&State=Disabled");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == FeatureFlagSource.Manual && !t.IsEnabled);
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenHasOverrideTrueAndNoTenantHasOverride_ShouldReturnEmpty()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?HasOverride=true");

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
        // Arrange - beta-features at 100% rollout means every tenant is "ab_rollout"; the manual
        // override for tenant-1 should be the only row HasOverride=true keeps.
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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().OnlyContain(t => t.Source == FeatureFlagSource.Manual);
        result.Tenants.Should().Contain(t => t.Id.Value == tenantId);
    }

    // Flag users query (regular PolicyName)

    [Fact]
    public async Task GetFeatureFlagUsers_WhenNoSearchProvided_ShouldReturnAllUsersPaginated()
    {
        // Arrange
        var flagKey = "compact-view";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users");

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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        result.Users.Should().OnlyContain(u => u.Source == FeatureFlagSource.Default);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenUserHasOverride_ShouldReturnUserWithManualOverrideSource()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().NotBeEmpty();
        var userResult = result.Users.Single(u => u.Id.Value == userId);
        userResult.IsEnabled.Should().BeTrue();
        userResult.Source.Should().Be(FeatureFlagSource.Manual);
        userResult.Email.Should().NotBe("Unknown");
        userResult.TenantName.Should().NotBe("Unknown");
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenBaseRowInactiveAndOverrideActive_ShouldReportIsEnabledFalse()
    {
        // Arrange — globally Deactivate compact-view (base row enabled_at=null) but keep an active
        // manual override row for the user. The runtime FeatureFlagEvaluator short-circuits on
        // !baseRow.IsActive (FeatureFlagEvaluator.cs:48) so this bulk list must mirror that contract.
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", null)]);
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?search=owner@tenant-1");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        var userResult = result.Users.Single(u => u.Id.Value == userId);
        userResult.IsEnabled.Should().BeFalse();
        userResult.Source.Should().Be(FeatureFlagSource.Manual);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenNonUserScopedFlag_ShouldReturnBadRequest()
    {
        using var client = CreateRegularBackOfficeClient();
        var response = await client.GetAsync("/api/back-office/feature-flags/sso/users");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSearchOnlyMatchingEmail_ShouldReturnOnlyMatchingUsers()
    {
        // Arrange
        var flagKey = "compact-view";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?Search=owner@tenant-1");

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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?Roles=Owner");

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
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?State=Enabled");

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
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?State=Disabled");

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
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?PageSize=1&PageOffset=0");

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
        using var client = CreateRegularBackOfficeClient();
        var response = await client.GetAsync("/api/back-office/feature-flags/compact-view/users?PageOffset=100");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideOmitted_ShouldNotFilterByOverride()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().Contain(u => u.Source == FeatureFlagSource.Manual);
        result.Users.Should().Contain(u => u.Source == FeatureFlagSource.Default);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideTrue_ShouldReturnOnlyUsersWithManualOverride()
    {
        // Arrange
        var flagKey = "compact-view";
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, userId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?HasOverride=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Should().OnlyContain(u => u.Source == FeatureFlagSource.Manual);
        result.Users.Should().Contain(u => u.Id.Value == userId);
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenHasOverrideTrueAndRoleFiltered_ShouldReturnOnlyMatchingUsersWithOverride()
    {
        // Arrange - only the owner gets the override.
        var flagKey = "compact-view";
        var ownerId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertUserOverride(flagKey, DatabaseSeeder.Tenant1.Id, ownerId, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var matchResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?HasOverride=true&Roles=Owner");
        var noMatchResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?HasOverride=true&Roles=Member");

        // Assert
        matchResponse.ShouldBeSuccessfulGetRequest();
        noMatchResponse.ShouldBeSuccessfulGetRequest();
        var matchResult = await matchResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        var noMatchResult = await noMatchResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        matchResult!.Users.Should().OnlyContain(u => u.Source == FeatureFlagSource.Manual && u.Role == UserRole.Owner);
        matchResult.Users.Should().Contain(u => u.Id.Value == ownerId);
        noMatchResult!.Users.Should().BeEmpty();
    }

    // List-all-flags query (regular PolicyName)

    [Fact]
    public async Task GetFeatureFlags_WhenCalled_ShouldReturnAllFlagsWithDatabaseState()
    {
        // Arrange
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().HaveCount(FeatureFlagRegistry.GetAll().Length);

        result.Flags.Single(f => f.Key == "google-oauth").Scope.Should().Be(FeatureFlagScope.System);
        result.Flags.Single(f => f.Key == "subscriptions").Scope.Should().Be(FeatureFlagScope.System);
        var betaFeatures = result.Flags.Single(f => f.Key == "beta-features");
        betaFeatures.Scope.Should().Be(FeatureFlagScope.Tenant);
        betaFeatures.IsAbTestEligible.Should().BeTrue();
        betaFeatures.IsActive.Should().BeTrue();
        result.Flags.Single(f => f.Key == "sso").IsActive.Should().BeFalse();
        result.Flags.Single(f => f.Key == "account-overview").ConfigurableByTenant.Should().BeTrue();
        result.Flags.Single(f => f.Key == "compact-view").ConfigurableByUser.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeatureFlags_WhenOrphanedRowWithScope_ShouldIncludeInResponse()
    {
        // Arrange
        InsertHistoricalBaseRow("removed-feature", FeatureFlagScope.Tenant);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagsResponse>();
        result.Should().NotBeNull();
        var orphan = result.Flags.Single(f => f.Key == "removed-feature");
        orphan.Scope.Should().Be(FeatureFlagScope.Tenant);
        orphan.OrphanedAt.Should().NotBeNull();
        orphan.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetFeatureFlags_WhenSoftDeletedRowAndIncludeDeletedFalse_ShouldExcludeFromResponse()
    {
        // Arrange
        InsertHistoricalBaseRow("removed-feature", FeatureFlagScope.Tenant, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/feature-flags");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().NotContain(f => f.Key == "removed-feature");
    }

    [Fact]
    public async Task GetFeatureFlags_WhenSoftDeletedRowAndIncludeDeletedTrue_ShouldIncludeWithDeletedAt()
    {
        // Arrange
        InsertHistoricalBaseRow("removed-feature", FeatureFlagScope.Tenant, true);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/feature-flags?IncludeDeleted=true");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagsResponse>();
        result.Should().NotBeNull();
        var deleted = result.Flags.Single(f => f.Key == "removed-feature");
        deleted.DeletedAt.Should().NotBeNull();
        deleted.OrphanedAt.Should().NotBeNull();
    }

    // Back-office mutations do NOT chain AddRefreshAuthenticationTokens() because the back-office
    // actor's own claim set is unchanged when they mutate flags for other tenants/users. Target
    // sessions pick up the change via the x-user-feature-flags header on their next request.

    [Fact]
    public async Task ActivateFeatureFlag_WhenCalledByAdmin_ShouldNotAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/activate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    [Fact]
    public async Task DeactivateFeatureFlag_WhenCalledByAdmin_ShouldNotAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.PutAsync($"/api/back-office/feature-flags/{flagKey}/deactivate", null);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    [Fact]
    public async Task SetTenantOverride_WhenCalled_ShouldNotAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "beta-features";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        using var client = CreateAdminBackOfficeClient();
        var command = new SetTenantFeatureFlagInternalCommand { TenantId = tenantId, Enabled = true };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    [Fact]
    public async Task SetRolloutPercentage_WhenCalled_ShouldNotAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "beta-features";
        using var client = CreateAdminBackOfficeClient();
        var command = new SetFeatureFlagRolloutPercentageCommand { RolloutPercentage = 50 };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/feature-flags/{flagKey}/rollout-percentage", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    // Delete feature flag (AdminPolicyName, orphan-only)

    [Fact]
    public async Task DeleteFeatureFlag_WhenOrphanedAndAdmin_ShouldSoftDeleteBaseAndHardDeleteOverridesAndEmitTelemetry()
    {
        // Arrange
        var flagKey = "removed-feature";
        var tenantId = DatabaseSeeder.Tenant1.Id;
        var userId = DatabaseSeeder.Tenant1Owner.Id.ToString();
        InsertOrphanedBaseRow(flagKey);
        InsertTenantOverride(flagKey, tenantId, true);
        InsertUserOverride(flagKey, tenantId, userId, true);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        var baseRowDeletedAt = Connection.ExecuteScalar<string>(
            "SELECT deleted_at FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        baseRowDeletedAt.Should().NotBeNullOrEmpty();

        var overrideCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND (tenant_id IS NOT NULL OR user_id IS NOT NULL)", [new { flagKey }]
        );
        overrideCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "FeatureFlagDeleted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.flag_key"].Should().Be(flagKey);
    }

    [Fact]
    public async Task DeleteFeatureFlag_WhenAlreadySoftDeleted_ShouldReturnBadRequest()
    {
        // Arrange
        var flagKey = "removed-feature";
        InsertOrphanedBaseRow(flagKey, true);
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteFeatureFlag_WhenNotOrphaned_ShouldReturnBadRequest()
    {
        // Arrange - sso is a live flag with OrphanedAt = NULL; hard-delete must be rejected.
        var flagKey = "sso";
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var baseRowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        baseRowCount.Should().Be(1, "a non-orphaned flag must not be deleted");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteFeatureFlag_WhenFlagDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateAdminBackOfficeClient();

        // Act
        var response = await client.DeleteAsync("/api/back-office/feature-flags/does-not-exist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFeatureFlag_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "removed-feature";
        InsertOrphanedBaseRow(flagKey);
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.DeleteAsync($"/api/back-office/feature-flags/{flagKey}");

        // Assert - the delete route is gated by AdminPolicyName, so a regular back-office identity without
        // the admin group claim must be rejected.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var baseRowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE flag_key = @flagKey", [new { flagKey }]
        );
        baseRowCount.Should().Be(1, "the orphaned row must remain when a non-admin caller is rejected");
    }

    // Server-side sort smoke tests

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSortedByNameAscendingAndDescending_ShouldReturnOppositeOrder()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var ascResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?OrderBy=Name&SortOrder=Ascending");
        var descResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?OrderBy=Name&SortOrder=Descending");

        // Assert
        ascResponse.ShouldBeSuccessfulGetRequest();
        descResponse.ShouldBeSuccessfulGetRequest();
        var ascResult = await ascResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        var descResult = await descResponse.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        ascResult.Should().NotBeNull();
        descResult.Should().NotBeNull();
        ascResult.Tenants.Should().NotBeEmpty();
        ascResult.Tenants.Select(t => t.Name).Should().BeInAscendingOrder();
        descResult.Tenants.Select(t => t.Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetFeatureFlagTenants_WhenSortedByPlan_ShouldReturnOrderedByPlan()
    {
        // Arrange
        var flagKey = "sso";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/tenants?OrderBy=Plan&SortOrder=Ascending");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagTenantsResponse>();
        result.Should().NotBeNull();
        result.Tenants.Select(t => t.Plan).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSortedByNameAscendingAndDescending_ShouldReturnOppositeOrder()
    {
        // Arrange
        var flagKey = "compact-view";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var ascResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?OrderBy=Name&SortOrder=Ascending");
        var descResponse = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?OrderBy=Name&SortOrder=Descending");

        // Assert
        ascResponse.ShouldBeSuccessfulGetRequest();
        descResponse.ShouldBeSuccessfulGetRequest();
        var ascResult = await ascResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        var descResult = await descResponse.DeserializeResponse<GetFeatureFlagUsersResponse>();
        ascResult.Should().NotBeNull();
        descResult.Should().NotBeNull();
        ascResult.Users.Should().NotBeEmpty();
        ascResult.Users.Select(u => u.Email).Should().Equal(descResult.Users.Select(u => u.Email).Reverse());
    }

    [Fact]
    public async Task GetFeatureFlagUsers_WhenSortedByRole_ShouldReturnOrderedByRole()
    {
        // Arrange
        var flagKey = "compact-view";
        using var client = CreateRegularBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/feature-flags/{flagKey}/users?OrderBy=Role&SortOrder=Ascending");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<GetFeatureFlagUsersResponse>();
        result.Should().NotBeNull();
        result.Users.Select(u => u.Role).Should().BeInAscendingOrder();
    }

    // The reconciler creates non-kill-switch base rows with EnabledAt=null until an admin Activates.
    // Tests that exercise override-driven IsEnabled need the base row Active so the mirror query
    // path is reached at all — FeatureFlagEvaluator.cs:48 short-circuits when baseRow.IsActive is false.
    private void ActivateBaseRow(string flagKey)
    {
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL", [new { flagKey }]
        );
        Connection.Update("feature_flags", "id", baseRowId, [("enabled_at", TimeProvider.System.GetUtcNow())]);
    }

    private void InsertOrphanedBaseRow(string flagKey, bool softDeleted = false)
    {
        InsertHistoricalBaseRow(flagKey, FeatureFlagScope.Tenant, softDeleted);
    }

    private void InsertHistoricalBaseRow(string flagKey, FeatureFlagScope scope, bool softDeleted = false)
    {
        var rowId = FeatureFlagId.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();
        Connection.Insert("feature_flags", [
                ("id", rowId),
                ("created_at", now),
                ("modified_at", null),
                ("deleted_at", softDeleted ? now : null),
                ("orphaned_at", now),
                ("flag_key", flagKey),
                ("tenant_id", null),
                ("user_id", null),
                ("enabled_at", now),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("source", "Manual"),
                ("scope", scope.ToString())
            ]
        );
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
                ("source", "Manual"),
                ("scope", "Tenant")
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
                ("source", "Manual"),
                ("scope", "User")
            ]
        );
    }

    private static int CountBucketsInRange(int rolloutBucketStart, int rolloutBucketEnd)
    {
        if (rolloutBucketStart <= rolloutBucketEnd) return rolloutBucketEnd - rolloutBucketStart + 1;
        return 100 - rolloutBucketStart + rolloutBucketEnd + 1;
    }
}
