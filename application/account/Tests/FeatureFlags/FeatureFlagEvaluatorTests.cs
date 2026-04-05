using Account.Database;
using Account.Features.FeatureFlags.Domain;
using Account.Features.FeatureFlags.Shared;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.FeatureFlags;

public sealed class FeatureFlagEvaluatorTests : EndpointBaseTest<AccountDbContext>
{
    private readonly FeatureFlagEvaluator _evaluationService;
    private readonly IServiceScope _scope;

    public FeatureFlagEvaluatorTests()
    {
        _scope = Provider.CreateScope();
        _evaluationService = _scope.ServiceProvider.GetRequiredService<FeatureFlagEvaluator>();
    }

    [Fact]
    public async Task Evaluate_WhenNoBaseFeatureFlag_ShouldReturnEmpty()
    {
        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WhenBaseFeatureFlagActiveWithTenantOverride_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, null, null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

        // Assert
        result.Should().Contain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenBaseFeatureFlagInactive_ShouldReturnEmpty()
    {
        // Arrange
        InsertFeatureFlag("sso", null, null, null, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    [Fact]
    public async Task Evaluate_WhenReactivated_EnabledAtAfterDisabledAt_ShouldReturnEnabled()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, now.AddMinutes(-5), null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 0, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 70, 0, CancellationToken.None);

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
        var resultInUpperRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 95, 0, CancellationToken.None);
        var resultInLowerRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 5, 0, CancellationToken.None);
        var resultOutOfRange = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 0, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 0, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

        // Assert
        result.Should().Contain("compact-view");
    }

    [Fact]
    public async Task Evaluate_WhenUserIdEmpty_ShouldSkipUserScopedFlags()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("compact-view", null, null, now, null, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, new UserId(""), 50, 50, CancellationToken.None);

        // Assert
        result.Should().NotContain("compact-view");
    }

    [Fact]
    public async Task Evaluate_WhenTenantOverrideDisabled_ShouldNotReturnFlag()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow();
        InsertFeatureFlag("sso", null, null, now, null, null, null);
        InsertFeatureFlag("sso", DatabaseSeeder.Tenant1.Id.Value, null, now.AddMinutes(-10), now, null, null);

        // Act
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

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
        var result = await _evaluationService.EvaluateAsync(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, 50, 50, CancellationToken.None);

        // Assert
        result.Should().NotContain("sso");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _scope.Dispose();
        base.Dispose(disposing);
    }

    private void InsertFeatureFlag(string featureFlagKey, long? tenantId, string? userId, DateTimeOffset? enabledAt, DateTimeOffset? disabledAt, int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        // Delete any seeded row with the same scope to avoid unique constraint conflicts
        using var deleteCommand = new SqliteCommand(
            tenantId is null && userId is null
                ? "DELETE FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id IS NULL AND user_id IS NULL"
                : "DELETE FROM feature_flags WHERE feature_flag_key = @featureFlagKey AND tenant_id = @tenantId AND user_id IS NULL",
            Connection
        );
        deleteCommand.Parameters.AddWithValue("@featureFlagKey", featureFlagKey);
        if (tenantId is not null)
        {
            deleteCommand.Parameters.AddWithValue("@tenantId", tenantId);
        }

        deleteCommand.ExecuteNonQuery();

        var id = FeatureFlagId.NewId().ToString();
        Connection.Insert("feature_flags", [
                ("id", id),
                ("feature_flag_key", featureFlagKey),
                ("tenant_id", tenantId),
                ("user_id", userId),
                ("created_at", TimeProvider.System.GetUtcNow()),
                ("modified_at", null),
                ("enabled_at", enabledAt),
                ("disabled_at", disabledAt),
                ("rollout_bucket_start", rolloutBucketStart),
                ("rollout_bucket_end", rolloutBucketEnd),
                ("configurable_by_tenant", false),
                ("configurable_by_user", false),
                ("source", "Manual")
            ]
        );
    }
}
