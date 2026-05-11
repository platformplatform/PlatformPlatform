using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

/// <summary>
///     Pins the data-retention guarantee that underwrites every dashboard query: Subscription rows are
///     immutable historical money facts and outlive the tenant lifecycle. Soft-deleting a tenant must not
///     cascade, must not flip the row to soft-deleted, and must not exclude the row from cross-tenant
///     back-office reads. Without this guarantee the MRR ledger silently corrupts on every churn.
/// </summary>
public sealed class SubscriptionRepositoryTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task WhenTenantIsSoftDeleted_SubscriptionRowsRemainQueryable()
    {
        // Seed a tenant with a paid subscription, soft-delete the tenant, then load the subscription via
        // the cross-tenant back-office paths. The subscription must still be returned because it is a
        // money fact that pre-dates the tenant deletion.
        // Arrange
        var tenantId = SeedTenant("Churned Co");
        var subscriptionId = SeedPaidSubscription(tenantId);
        Connection.Update("tenants", "id", tenantId.Value, [("deleted_at", TimeProvider.GetUtcNow())]);

        using var scope = WebApplicationServices.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        // Act
        var byTenantId = await subscriptionRepository.GetByTenantIdUnfilteredAsync(tenantId, CancellationToken.None);
        var byTenantIds = await subscriptionRepository.GetByTenantIdsUnfilteredAsync([tenantId], CancellationToken.None);
        var allActive = await subscriptionRepository.GetAllActiveUnfilteredAsync(CancellationToken.None);

        // Assert
        byTenantId.Should().NotBeNull("the subscription row must survive tenant soft-delete");
        byTenantId.Id.Should().Be(subscriptionId);
        byTenantIds.Should().ContainSingle(s => s.Id == subscriptionId);
        allActive.Should().Contain(s => s.Id == subscriptionId, "the back-office MRR snapshot must include subscriptions of soft-deleted tenants");
    }

    private TenantId SeedTenant(string name)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private SubscriptionId SeedPaidSubscription(TenantId tenantId)
    {
        var subscriptionId = SubscriptionId.NewId();
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", 99m),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
        return subscriptionId;
    }
}
