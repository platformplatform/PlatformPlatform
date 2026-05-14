using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

/// <summary>
///     Pins the data-retention guarantee for the BillingEvent log: rows are append-only and immutable, and
///     they outlive the tenant lifecycle. Soft-deleting a tenant must not cascade, must not flip the row to
///     soft-deleted, and must not exclude the row from cross-tenant back-office reads. Without this guarantee
///     the historical MRR trend silently rewrites itself every time a tenant churns.
/// </summary>
public sealed class BillingEventRepositoryTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    [Fact]
    public async Task WhenTenantIsSoftDeleted_BillingEventRowsRemainQueryable()
    {
        // Seed a tenant with a subscription and an MRR-changing billing event, soft-delete the tenant,
        // then load the events via the cross-tenant back-office paths. The billing event row must still
        // be returned because it is an immutable money fact that pre-dates the tenant deletion.
        // Arrange
        var tenantId = SeedTenant("Churned Co");
        var subscriptionId = SubscriptionId.NewId();
        var billingEventId = SeedSubscriptionCreatedEvent(tenantId, subscriptionId, 99m);
        Connection.Update("tenants", "id", tenantId.Value, [("deleted_at", TimeProvider.GetUtcNow())]);

        using var scope = WebApplicationServices.CreateScope();
        var billingEventRepository = scope.ServiceProvider.GetRequiredService<IBillingEventRepository>();

        // Act
        var bySubscriptionId = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscriptionId, CancellationToken.None);
        var mrrChangeEvents = await billingEventRepository.GetMrrChangeEventsUnfilteredAsync(CancellationToken.None);
        var recent = await billingEventRepository.GetRecentUnfilteredAsync(10, CancellationToken.None);

        // Assert
        bySubscriptionId.Should().ContainSingle(e => e.Id == billingEventId, "the billing event row must survive tenant soft-delete");
        mrrChangeEvents.Should().Contain(e => e.Id == billingEventId, "the historical MRR trend must include events from soft-deleted tenants");
        recent.Should().Contain(e => e.Id == billingEventId);
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
                ("logo", """{"Url":null,"Version":0}"""),
                ("rollout_bucket", 0)
            ]
        );
        return tenantId;
    }

    private BillingEventId SeedSubscriptionCreatedEvent(TenantId tenantId, SubscriptionId subscriptionId, decimal newAmount)
    {
        var occurredAt = TimeProvider.GetUtcNow().AddDays(-30);
        var billingEventId = BillingEventId.NewId();
        Connection.Insert("billing_events", [
                ("tenant_id", tenantId.Value),
                ("id", billingEventId.Value),
                ("subscription_id", subscriptionId.Value),
                ("created_at", occurredAt),
                ("modified_at", null),
                ("stripe_event_id", $"evt_test_{Guid.NewGuid():N}"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Premium)),
                ("previous_amount", 0m),
                ("new_amount", newAmount),
                ("amount_delta", newAmount),
                ("committed_mrr", newAmount),
                ("currency", MockStripeClient.MockStandardCurrency),
                ("occurred_at", occurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );
        return billingEventId;
    }
}
