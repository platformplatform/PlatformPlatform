using Account.Database;
using Account.Features.FeatureFlags.Shared;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.FeatureFlags;

public sealed class PlanBasedFeatureFlagEvaluatorTests : EndpointBaseTest<AccountDbContext>
{
    private readonly AccountDbContext _dbContext;
    private readonly PlanBasedFeatureFlagEvaluator _service;

    public PlanBasedFeatureFlagEvaluatorTests()
    {
        var scope = Provider.CreateScope();
        _service = scope.ServiceProvider.GetRequiredService<PlanBasedFeatureFlagEvaluator>();
        _dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    }

    [Fact]
    public async Task EvaluatePlanFlags_WhenPremiumPlan_ShouldActivatePremiumFlags()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        await _service.EvaluatePlanFlagsForTenantAsync(tenantId, SubscriptionPlan.Premium, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Assert
        var ssoEnabled = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );
        ssoEnabled.Should().NotBeNullOrEmpty();

        var source = Connection.ExecuteScalar<string>(
            "SELECT source FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );
        source.Should().Be("Plan");
    }

    [Fact]
    public async Task EvaluatePlanFlags_WhenFreePlan_ShouldDeactivatePremiumFlags()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;
        await _service.EvaluatePlanFlagsForTenantAsync(tenantId, SubscriptionPlan.Premium, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.EvaluatePlanFlagsForTenantAsync(tenantId, SubscriptionPlan.Basis, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Assert
        var disabledAt = Connection.ExecuteScalar<string>(
            "SELECT disabled_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );
        disabledAt.Should().NotBeNullOrEmpty();

        var enabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );
        enabledAt.Should().NotBeNullOrEmpty("EnabledAt should be preserved on deactivation");
    }

    [Fact]
    public async Task EvaluatePlanFlags_WhenAlreadyActive_ShouldBeIdempotent()
    {
        // Arrange
        var tenantId = DatabaseSeeder.Tenant1.Id;
        await _service.EvaluatePlanFlagsForTenantAsync(tenantId, SubscriptionPlan.Premium, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var firstEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );

        // Act
        await _service.EvaluatePlanFlagsForTenantAsync(tenantId, SubscriptionPlan.Premium, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Assert
        var secondEnabledAt = Connection.ExecuteScalar<string>(
            "SELECT enabled_at FROM feature_flags WHERE flag_key = 'sso' AND tenant_id = @tenantId AND user_id IS NULL",
            [new { tenantId = tenantId.Value }]
        );
        secondEnabledAt.Should().Be(firstEnabledAt);
    }
}
