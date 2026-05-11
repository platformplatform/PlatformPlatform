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
///     Scope coverage for the predicate that drives <c>BillingDriftWorker</c>. The worker must scan every
///     subscription whose drift check is stale and whose StripeCustomerId is non-null, regardless of Plan —
///     this is what guarantees that cancelled tenants (Plan reset to Basis, customer id retained, billing
///     history intact) keep being audited for drift instead of silently falling off the radar.
/// </summary>
public sealed class SubscriptionRepositoryDriftScopeTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetSubscriptionsDueForDriftCheckUnfilteredAsync_WhenSubscriptionsHaveMixedStaleness_ShouldReturnOnlyStaleOnesWithStripeCustomer()
    {
        // Arrange — three subscriptions: one stale + has Stripe customer (should be returned), one freshly
        // checked (should be skipped), one with no Stripe customer id (should be skipped — nothing to compare).
        var cutoff = TimeProvider.GetUtcNow().AddHours(-23);

        var staleTenantId = SeedTenant("Stale Co");
        var staleSubscriptionId = SeedSubscription(staleTenantId, "cus_stale", null);

        var freshTenantId = SeedTenant("Fresh Co");
        SeedSubscription(freshTenantId, "cus_fresh", TimeProvider.GetUtcNow().AddMinutes(-5));

        var noCustomerTenantId = SeedTenant("No Customer Co", nameof(SubscriptionPlan.Basis));
        SeedSubscription(noCustomerTenantId, null, null, nameof(SubscriptionPlan.Basis));

        using var scope = WebApplicationServices.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        // Act
        var dueSubscriptions = await subscriptionRepository.GetSubscriptionsDueForDriftCheckUnfilteredAsync(cutoff, CancellationToken.None);

        // Assert — only the stale + has-customer row is returned. The freshly checked row and the row without
        // a Stripe customer id are both excluded.
        dueSubscriptions.Should().ContainSingle(s => s.Id == staleSubscriptionId);
    }

    [Fact]
    public async Task GetSubscriptionsDueForDriftCheckUnfilteredAsync_WhenSubscriptionIsCancelledButRetainsStripeCustomerId_ShouldStillBeReturned()
    {
        // Arrange — regression guard: a tenant that cancelled (Plan reset to Basis) still has stripe_customer_id
        // retained and a stale DriftCheckedAt. The legacy sweeper queried GetAllActiveUnfilteredAsync which
        // filters by Plan != Basis and silently skipped cancelled tenants — drift in their historical event
        // log was never detected. The new predicate is plan-agnostic; it must return this subscription.
        var cutoff = TimeProvider.GetUtcNow().AddHours(-23);

        var cancelledTenantId = SeedTenant("Cancelled Co", nameof(SubscriptionPlan.Basis));
        var cancelledSubscriptionId = SeedSubscription(cancelledTenantId, "cus_cancelled", null, nameof(SubscriptionPlan.Basis));

        using var scope = WebApplicationServices.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        // Act
        var dueSubscriptions = await subscriptionRepository.GetSubscriptionsDueForDriftCheckUnfilteredAsync(cutoff, CancellationToken.None);

        // Assert — a cancelled tenant with a retained Stripe customer id must still be returned.
        dueSubscriptions.Should().Contain(s => s.Id == cancelledSubscriptionId);
    }

    private TenantId SeedTenant(string name, string plan = nameof(SubscriptionPlan.Premium))
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", plan),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private SubscriptionId SeedSubscription(TenantId tenantId, string? stripeCustomerId, DateTimeOffset? driftCheckedAt, string plan = nameof(SubscriptionPlan.Premium))
    {
        var subscriptionId = SubscriptionId.NewId();
        var hasStripeSubscription = stripeCustomerId is not null && plan != nameof(SubscriptionPlan.Basis);
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("plan", plan),
                ("scheduled_plan", null),
                ("stripe_customer_id", stripeCustomerId),
                ("stripe_subscription_id", hasStripeSubscription ? "sub_test" : null),
                ("current_price_amount", hasStripeSubscription ? 29.99m : (decimal?)null),
                ("current_price_currency", hasStripeSubscription ? "DKK" : null),
                ("current_period_end", hasStripeSubscription ? TimeProvider.GetUtcNow().AddDays(30) : (DateTimeOffset?)null),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", driftCheckedAt),
                ("drift_discrepancies", "[]")
            ]
        );
        return subscriptionId;
    }
}
