extern alias workers;
using Account.Database;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Users.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Tests.Persistence;
using Xunit;
using FeatureFlagDefinitionReconciler = workers::Account.Workers.FeatureFlagDefinitionReconciler;

namespace Account.Tests.Workers;

/// <summary>
///     Backend acceptance tests for the FeatureFlagDefinitionReconciler per PP-1250. The reconciler runs
///     inline at Worker startup and converges the feature_flags table to the C# definitions in
///     <c>SharedKernel.FeatureFlags.FeatureFlags</c>. These tests exercise the reconciler against the
///     production code path that the existing test harness's hand-built fixtures do not cover.
/// </summary>
public sealed class FeatureFlagDefinitionReconcilerTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task Reconciler_WhenPremiumTenantWithPlanOverride_ShouldEnableSsoInUserInfo()
    {
        // Arrange - simulate the production state after a Premium subscription: ProcessPendingStripeEvents
        // would have committed a Source=Plan tenant override via MediatR UnitOfWork. We pre-insert that row
        // here to verify the reconciler's contract: when the override row exists in the DB, the resulting
        // UserInfo MUST carry sso. The reconciler's job is to ensure the base row is active+Source=Plan so
        // the evaluator doesn't short-circuit at the base-row gate (the original C1 bug).
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [("plan", nameof(SubscriptionPlan.Premium))]);
        Connection.Insert("feature_flags", [
                ("id", FeatureFlagId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("deleted_at", null),
                ("orphaned_at", null),
                ("flag_key", "sso"),
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Plan"),
                ("scope", "Tenant")
            ]
        );

        await RunReconcilerAsync();

        using var scope = Provider.CreateScope();
        var userInfoFactory = scope.ServiceProvider.GetRequiredService<UserInfoFactory>();

        // Act
        var userInfoResult = await userInfoFactory.CreateUserInfoAsync(DatabaseSeeder.Tenant1Owner, DatabaseSeeder.Tenant1OwnerSession.Id, CancellationToken.None);

        // Assert
        userInfoResult.IsSuccess.Should().BeTrue();
        userInfoResult.Value!.FeatureFlags.Should().Contain("sso", "Premium tenants with a Plan-source override must have sso in their JWT feature_flags claim");
    }

    [Fact]
    public async Task Reconciler_WhenBasisTenantAndLogin_ShouldNotEnableSsoInUserInfo()
    {
        // Arrange - the seeder defaults the subscription plan to Basis, so no Plan-source override exists.
        await RunReconcilerAsync();

        using var scope = Provider.CreateScope();
        var userInfoFactory = scope.ServiceProvider.GetRequiredService<UserInfoFactory>();

        // Act
        var userInfoResult = await userInfoFactory.CreateUserInfoAsync(DatabaseSeeder.Tenant1Owner, DatabaseSeeder.Tenant1OwnerSession.Id, CancellationToken.None);

        // Assert
        userInfoResult.IsSuccess.Should().BeTrue();
        userInfoResult.Value!.FeatureFlags.Should().NotContain("sso", "Basis tenants must not have sso in their JWT feature_flags claim");
    }

    [Fact]
    public async Task Reconciler_WhenFlagFlipsFromManualToPlan_ShouldRemoveStaleManualTenantOverride()
    {
        // Arrange - the test seeder created the sso base row with default Source=Manual. Add a Manual
        // tenant override for a Basis tenant to simulate the pre-PP-1250 state where the flag was
        // operator-set rather than plan-derived.
        var staleOverrideId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", staleOverrideId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("deleted_at", null),
                ("orphaned_at", null),
                ("flag_key", "sso"),
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual"),
                ("scope", "Tenant")
            ]
        );

        // Act
        await RunReconcilerAsync();

        // Assert
        Connection.RowExists("feature_flags", staleOverrideId).Should().BeFalse("the reconciler must remove Manual-source rows for a flag whose definition now requires Plan source");

        var baseRowSource = Connection.ExecuteScalar<string>(
            "SELECT source FROM feature_flags WHERE flag_key = 'sso' AND tenant_id IS NULL AND user_id IS NULL", []
        );
        baseRowSource.Should().Be("Plan", "the reconciler must transition the base row to the expected Plan source");
    }

    [Fact]
    public async Task Reconciler_WhenRunTwice_ShouldBeIdempotent()
    {
        // Arrange - first pass converges the seeder rows
        await RunReconcilerAsync();
        var rowCountAfterFirstPass = Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM feature_flags", []);
        // Capture a per-row fingerprint that changes if any column the reconciler ever writes changes.
        // A bare COUNT(*) assertion would miss a regression that re-issues Update on every pass without
        // adding or deleting rows; this fingerprint catches that case.
        var fingerprintAfterFirstPass = Connection.ExecuteScalar<string>(
            "SELECT GROUP_CONCAT(id || ':' || COALESCE(modified_at, '') || ':' || COALESCE(enabled_at, '') || ':' || COALESCE(disabled_at, '') || ':' || source || ':' || COALESCE(orphaned_at, '')) FROM feature_flags ORDER BY id", []
        );

        // Act
        await RunReconcilerAsync();

        // Assert
        var rowCountAfterSecondPass = Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM feature_flags", []);
        rowCountAfterSecondPass.Should().Be(rowCountAfterFirstPass, "a second reconciler pass must not insert or delete rows");

        var fingerprintAfterSecondPass = Connection.ExecuteScalar<string>(
            "SELECT GROUP_CONCAT(id || ':' || COALESCE(modified_at, '') || ':' || COALESCE(enabled_at, '') || ':' || COALESCE(disabled_at, '') || ':' || source || ':' || COALESCE(orphaned_at, '')) FROM feature_flags ORDER BY id", []
        );
        fingerprintAfterSecondPass.Should().Be(fingerprintAfterFirstPass, "a second reconciler pass must not modify any row (modified_at, enabled_at, disabled_at, source, orphaned_at all unchanged)");
    }

    [Fact]
    public async Task Reconciler_WhenRowExistsForRemovedFlag_ShouldMarkOrphaned()
    {
        // Arrange - insert a row whose flag_key is not in FeatureFlags.cs (simulating a flag removed
        // from the C# definitions between deploys).
        var orphanRowId = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", orphanRowId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("deleted_at", null),
                ("orphaned_at", null),
                ("flag_key", "removed-feature"),
                ("tenant_id", null),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual"),
                ("scope", "Tenant")
            ]
        );

        // Act
        await RunReconcilerAsync();

        // Assert
        var orphanedAt = Connection.ExecuteScalar<string>(
            "SELECT orphaned_at FROM feature_flags WHERE id = @id", [new { id = orphanRowId }]
        );
        orphanedAt.Should().NotBeNullOrEmpty("the reconciler must mark rows whose flag_key is no longer in the C# definitions");

        var ssoOrphanedAt = Connection.ExecuteScalar<string>(
            "SELECT orphaned_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id IS NULL AND user_id IS NULL", []
        );
        ssoOrphanedAt.Should().BeNullOrEmpty("known flag rows must never be marked orphaned");
    }

    [Fact]
    public async Task Reconciler_WhenOrphanedBaseRowKeyIsBackInDefinitions_ShouldClearOrphanedAt()
    {
        // Arrange
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = 'sso' AND tenant_id IS NULL AND user_id IS NULL", []
        );
        Connection.Update("feature_flags", "id", baseRowId, [("orphaned_at", TimeProvider.GetUtcNow())]);

        // Act
        await RunReconcilerAsync();

        // Assert
        var orphanedAt = Connection.ExecuteScalar<string>(
            "SELECT orphaned_at FROM feature_flags WHERE id = @id", [new { id = baseRowId }]
        );
        orphanedAt.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Reconciler_WhenSoftDeletedBaseRowKeyIsBackInDefinitions_ShouldAbortDeployment()
    {
        // Arrange
        var baseRowId = Connection.ExecuteScalar<string>(
            "SELECT id FROM feature_flags WHERE flag_key = 'sso' AND tenant_id IS NULL AND user_id IS NULL", []
        );
        Connection.Update("feature_flags", "id", baseRowId, [
                ("orphaned_at", TimeProvider.GetUtcNow()),
                ("deleted_at", TimeProvider.GetUtcNow())
            ]
        );

        // Act
        var act = async () => await RunReconcilerAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Feature flag 'sso' was previously deleted*");
        var deletedAt = Connection.ExecuteScalar<string>(
            "SELECT deleted_at FROM feature_flags WHERE id = @id", [new { id = baseRowId }]
        );
        deletedAt.Should().NotBeNullOrEmpty("the reconciler must not clear DeletedAt; it must fail deployment instead");
    }

    private async Task RunReconcilerAsync()
    {
        using var scope = Provider.CreateScope();
        var featureFlagRepository = scope.ServiceProvider.GetRequiredService<IFeatureFlagRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var reconciler = new FeatureFlagDefinitionReconciler(featureFlagRepository, dbContext, TimeProvider, NullLogger<FeatureFlagDefinitionReconciler>.Instance);
        await reconciler.ReconcileAsync(CancellationToken.None);
    }
}
