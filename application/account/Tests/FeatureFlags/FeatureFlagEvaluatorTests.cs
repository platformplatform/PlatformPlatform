using Account.Database;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Shared;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.FeatureFlags;

public sealed class FeatureFlagEvaluatorTests : EndpointBaseTest<AccountDbContext>, IClassFixture<AccountWebApplicationFactory>
{
    private readonly FeatureFlagEvaluator _evaluationService;
    private readonly IServiceScope _scope;

    public FeatureFlagEvaluatorTests(AccountWebApplicationFactory factory) : base(factory)
    {
        _scope = Provider.CreateScope();
        _evaluationService = _scope.ServiceProvider.GetRequiredService<FeatureFlagEvaluator>();
    }

    [Fact]
    public async Task Evaluate_WhenNoBaseRow_ShouldReturnEmpty()
    {
        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WhenBaseRowActiveWithTenantOverride_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, null, null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().Contain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenBaseRowInactive_ShouldReturnEmpty()
    {
        // Arrange
        InsertFeatureFlag("sso", null, null, null, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenManualOverrideEnabledAndBaseRowInactive_ShouldExcludeFlag()
    {
        // Arrange — beta-features is a kill-switch flag and the base row is left inactive (the panic
        // button is pulled). The tenant has an active manual override. Per the documented precedence
        // chain, an inactive base row short-circuits before the manual-override step is reached so
        // pulling the kill switch is truly global, even for tenants admins had explicitly opted in.
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, null, null, null, null);
        InsertFeatureFlag("beta-features", DatabaseSeeder.Tenant1.Id.Value, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenReactivated_EnabledAtAfterDisabledAt_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, now.AddMinutes(-5), null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().Contain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenDisabledAtAfterEnabledAt_ShouldReturnEmpty()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now.AddMinutes(-10), now, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenAbTestEligibleAndBucketInRange_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, null, null, CancellationToken.None);

        // Assert
        result.Should().Contain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenAbTestEligibleAndBucketOutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 70, null, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenBucketWrapAround_ShouldIncludeWrappedRange()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 90, 10);

        // Act
        var resultInUpperRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 95, null, null, null, CancellationToken.None);
        var resultInLowerRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 5, null, null, null, CancellationToken.None);
        var resultOutOfRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, null, null, CancellationToken.None);

        // Assert
        resultInUpperRange.Should().Contain("beta-features");
        resultInLowerRange.Should().Contain("beta-features");
        resultOutOfRange.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenNullBucketStartAndEnd_ShouldNotEnableViaRollout()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenUserOverrideActive_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("compact-view", null, null, now, null, null, null);
        InsertFeatureFlag("compact-view", DatabaseSeeder.Tenant1.Id.Value, DatabaseSeeder.Tenant1Owner.Id.Value, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().Contain("compact-view");
    }

    [Fact]
    public async Task Evaluate_WhenTenantOverrideDisabled_ShouldNotReturnFlag()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, null, null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now.AddMinutes(-10), now, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenTenantOverrideExistsForDifferentTenant_ShouldNotReturnFlag()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        var otherTenantId = TenantId.NewId().Value;
        InsertFeatureFlag("sso", null, null, now, null, null, null);
        InsertFeatureFlag("sso", otherTenantId, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenTenantPinAlwaysOnAndBucketOutOfRange_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 70, null, AbInclusionPin.AlwaysOn, null, CancellationToken.None);

        // Assert
        result.Should().Contain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenTenantPinNeverOnAndBucketInRange_ShouldReturnEmpty()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, AbInclusionPin.NeverOn, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenManualOverrideAndTenantPinDisagree_ManualOverrideWins()
    {
        // Arrange — manual override disables, pin says AlwaysOn. Manual must win.
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 40, 60);
        InsertFeatureFlag("beta-features", DatabaseSeeder.Tenant1.Id.Value, null, now.AddMinutes(-10), now, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, AbInclusionPin.AlwaysOn, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenUserPinAlwaysOnAndBucketOutOfRange_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("experimental-ui", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 70, null, AbInclusionPin.AlwaysOn, CancellationToken.None);

        // Assert
        result.Should().Contain("experimental-ui");
    }

    [Fact]
    public async Task Evaluate_WhenUserPinNeverOnAndBucketInRange_ShouldReturnEmpty()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("experimental-ui", null, null, now, null, 40, 60);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, AbInclusionPin.NeverOn, CancellationToken.None);

        // Assert
        result.Should().NotContain("experimental-ui");
    }

    [Fact]
    public async Task Evaluate_WhenTenantPinAlwaysOnAndRolloutZeroPercent_ShouldStillIncludeTenant()
    {
        // Arrange — 0% rollout writes BucketStart=BucketEnd=NULL. Pre-fix, the rollout-null short-circuit
        // would silently disable an AlwaysOn-pinned tenant. Pins must trump rollout: an admin who pins a
        // tenant ahead of launch and leaves the rollout at 0% must still see the flag for that tenant.
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, AbInclusionPin.AlwaysOn, null, CancellationToken.None);

        // Assert
        result.Should().Contain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenTenantPinNeverOnAndRolloutHundredPercent_ShouldStillExcludeTenant()
    {
        // Arrange — 100% rollout (BucketStart=0, BucketEnd=99) covers every bucket. Pre-fix, the
        // pin-as-synthetic-bucket logic landed NeverOn in the covered range and the tenant was included
        // anyway. Pins must trump rollout: NeverOn excludes the tenant regardless of rollout percentage.
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 0, 99);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, AbInclusionPin.NeverOn, null, CancellationToken.None);

        // Assert
        result.Should().NotContain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenUserPinAlwaysOnAndRolloutZeroPercent_ShouldStillIncludeUser()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("experimental-ui", null, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, AbInclusionPin.AlwaysOn, CancellationToken.None);

        // Assert
        result.Should().Contain("experimental-ui");
    }

    [Fact]
    public async Task Evaluate_WhenUserPinNeverOnAndRolloutHundredPercent_ShouldStillExcludeUser()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("experimental-ui", null, null, now, null, 0, 99);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, AbInclusionPin.NeverOn, CancellationToken.None);

        // Assert
        result.Should().NotContain("experimental-ui");
    }

    [Fact]
    public async Task Evaluate_WhenTenantPinAlwaysOnAndBucketStartIsZero_ShouldIncludeAtOnePercentRollout()
    {
        // Arrange — AlwaysOn pin must map to BucketStart, which is the first slot in the rollout sequence
        // (threshold 1%). A 1% rollout starting at bucket 0 covers exactly bucket 0, so a pinned tenant
        // whose actual bucket is outside that range must still resolve to enabled.
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("beta-features", null, null, now, null, 0, 0);

        // Act — pass a tenant bucket that is far from 0 to prove the pin (not the bucket) drove the result.
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, null, AbInclusionPin.AlwaysOn, null, CancellationToken.None);

        // Assert
        result.Should().Contain("beta-features");
    }

    [Fact]
    public async Task Evaluate_WhenChildHasParentDependencyAndParentBaseRowInactive_ShouldExcludeChild()
    {
        // Arrange — override the evaluator's definitions provider with a stub topology that pairs a
        // parent flag with a parent-dependent child. The production registry does not currently declare
        // any parent-dep relationship, so this exercises the gating in FeatureFlagEvaluator without
        // contributing test-only flags to the real registry.
        var parent = new TenantAbTestFlag("test-parent", "Test parent", "Parent flag for evaluator test", false, false);
        var child = new TenantAbTestFlag("test-child", "Test child", "Child flag for evaluator test", false, false, "test-parent");
        var evaluator = new FeatureFlagEvaluator(_scope.ServiceProvider.GetRequiredService<IFeatureFlagRepository>())
        {
            DefinitionsProvider = () => [parent, child]
        };

        var now = TimeProvider.System.GetUtcNow();
        // Parent base row exists but is inactive (EnabledAt is null).
        InsertFeatureFlag(parent.Key, null, null, null, null, null, null);
        // Child base row is active with a rollout that would otherwise include the tenant.
        InsertFeatureFlag(child.Key, null, null, now, null, 0, 99);

        // Act
        var result = await evaluator.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, null, null, CancellationToken.None);

        // Assert — child must be gated by its parent and excluded when the parent is inactive.
        result.Should().NotContain(child.Key);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _scope.Dispose();
        base.Dispose(disposing);
    }

    private void InsertFeatureFlag(string flagKey, long? tenantId, string? userId, DateTimeOffset? enabledAt, DateTimeOffset? disabledAt, int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        // Delete any seeded row with the same scope to avoid unique constraint conflicts. The guard
        // below ensures the helper only ever clears a single seed row — if a future migration introduces
        // multiple seeded rows for the same (key, tenant, scope) tuple, the test fails fast instead of
        // silently overwriting them.
        using var deleteCommand = new SqliteCommand(
            tenantId is null && userId is null
                ? "DELETE FROM feature_flags WHERE flag_key = @flagKey AND tenant_id IS NULL AND user_id IS NULL"
                : "DELETE FROM feature_flags WHERE flag_key = @flagKey AND tenant_id = @tenantId AND user_id IS NULL",
            Connection
        );
        deleteCommand.Parameters.AddWithValue("@flagKey", flagKey);
        if (tenantId is not null)
        {
            deleteCommand.Parameters.AddWithValue("@tenantId", tenantId);
        }

        var rowsDeleted = deleteCommand.ExecuteNonQuery();
        if (rowsDeleted > 1)
        {
            throw new InvalidOperationException($"InsertFeatureFlag deleted '{rowsDeleted}' seeded rows for key '{flagKey}'; expected at most 1. Update the helper if the seed contract changed.");
        }

        var id = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", id),
                ("flag_key", flagKey),
                ("tenant_id", tenantId),
                ("user_id", userId),
                ("created_at", TimeProvider.System.GetUtcNow()),
                ("modified_at", null),
                ("enabled_at", enabledAt),
                ("disabled_at", disabledAt),
                ("bucket_start", rolloutBucketStart),
                ("bucket_end", rolloutBucketEnd),
                ("source", "Manual"),
                ("scope", tenantId is null && userId is null ? "Tenant" : userId is not null ? "User" : "Tenant")
            ]
        );
    }
}
