using System.Net;
using Account.Database;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class DeleteTenantTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{unknownTenantId}");

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenValid_ShouldSoftDeleteTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{existingTenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("tenants", existingTenantId).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM tenants WHERE id = @id", [new { id = existingTenantId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();

        var ownerDeletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]);
        ownerDeletedAt.Should().BeNull();
        var memberDeletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Member.Id.ToString() }]);
        memberDeletedAt.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantHasFeatureFlagOverrides_ShouldRemoveOnlyDeletedTenantRowsAndReportRowCount()
    {
        // Arrange — seed two tenants each with one Manual and one Plan override on a shared key, then
        // delete tenant A and verify only A's rows are gone, B's survive, and the TenantDeleted event
        // reports the correct feature_flag_rows_removed count.
        var deletedTenantId = DatabaseSeeder.Tenant1.Id;
        var survivingTenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", survivingTenantId.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", "Surviving tenant"),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );
        InsertTenantFeatureFlagOverride("sso", deletedTenantId, "Plan");
        InsertTenantFeatureFlagOverride("beta-features", deletedTenantId, "Manual");
        InsertTenantFeatureFlagOverride("sso", survivingTenantId, "Plan");
        InsertTenantFeatureFlagOverride("beta-features", survivingTenantId, "Manual");

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{deletedTenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var deletedTenantRowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE tenant_id = @tenantId", [new { tenantId = deletedTenantId.Value }]
        );
        deletedTenantRowCount.Should().Be(0, "the cascade must remove every feature_flag row owned by the deleted tenant");

        var survivingTenantRowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM feature_flags WHERE tenant_id = @tenantId", [new { tenantId = survivingTenantId.Value }]
        );
        survivingTenantRowCount.Should().Be(2, "other tenants' overrides must not be touched");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantDeleted");
        var tenantDeleted = TelemetryEventsCollectorSpy.CollectedEvents.Single(e => e.GetType().Name == "TenantDeleted");
        tenantDeleted.Properties["event.feature_flag_rows_removed"].Should().Be("2");
    }

    private void InsertTenantFeatureFlagOverride(string flagKey, TenantId tenantId, string source)
    {
        Connection.Insert("feature_flags", [
                ("id", FeatureFlagId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("flag_key", flagKey),
                ("tenant_id", tenantId.Value),
                ("user_id", null),
                ("enabled_at", TimeProvider.GetUtcNow()),
                ("disabled_at", null),
                ("bucket_start", null),
                ("bucket_end", null),
                ("source", source),
                ("scope", "Tenant")
            ]
        );
    }

    [Fact]
    public async Task DeleteTenant_WhenActiveSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", "cus_test_123"),
                ("stripe_subscription_id", "sub_test_123"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{DatabaseSeeder.Tenant1.Id}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Cannot delete a tenant with an active subscription.");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
