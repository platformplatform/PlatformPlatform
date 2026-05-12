using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Authentication.Domain;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardKpisTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardKpis_WhenCalled_ShouldReturnTenantUserAndRevenueAggregates()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var activeTenant = SeedTenant("Active Inc", SubscriptionPlan.Standard, now.AddDays(-60));
        SeedSubscription(activeTenant, SubscriptionPlan.Standard, 49.99m, true);
        SeedUser(activeTenant, "active-user@example.com", now.AddDays(-50));

        var trialTenant = SeedTenant("Trial Co", SubscriptionPlan.Basis, now.AddDays(-2));
        SeedUser(trialTenant, "trial-user@example.com", now.AddDays(-2));

        var canceledTenant = SeedTenant("Canceled Ltd", SubscriptionPlan.Basis, now.AddDays(-90));
        SeedSubscription(canceledTenant, SubscriptionPlan.Basis, null, true);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder seeds Tenant1 (Basis, no payments) plus the three tenants seeded by this test.
        payload.TotalTenants.Should().Be(4);
        payload.ActiveTenants.Should().Be(1);
        // Trial = state Active && plan Basis && never paid. DatabaseSeeder Tenant1 also matches this definition.
        payload.TrialTenants.Should().Be(2);
        payload.CanceledTenants.Should().Be(1);
        payload.BlendedMonthlyRecurringRevenue.Should().Be(49.99m);
        payload.Currency.Should().Be(MockStripeClient.MockStandardCurrency);
        payload.Period.Should().Be(DashboardTrendPeriod.Last30Days);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenCalled_ShouldReturnUserAndSessionAggregatesAcrossTenants()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var firstTenant = SeedTenant("First Tenant", SubscriptionPlan.Basis, now.AddDays(-10));
        var secondTenant = SeedTenant("Second Tenant", SubscriptionPlan.Standard, now.AddDays(-40));
        SeedSubscription(secondTenant, SubscriptionPlan.Standard, 100.00m, true);
        var firstTenantUser = SeedUser(firstTenant, "first@example.com", now.AddDays(-5));
        var secondTenantUser = SeedUser(secondTenant, "second@example.com", now.AddDays(-35));
        SeedSession(firstTenant, firstTenantUser, now.AddHours(-3), false);
        SeedSession(secondTenant, secondTenantUser, now.AddHours(-2), false);
        SeedSession(secondTenant, secondTenantUser, now.AddHours(-1), true);
        SeedSession(secondTenant, secondTenantUser, now.AddDays(-2), false);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder seeds 2 users in Tenant1 plus 2 users seeded above.
        payload.TotalUsers.Should().Be(4);
        // Only sessions created within the last 24 hours and not revoked count. DatabaseSeeder also seeds two
        // active sessions for Tenant1 within the last 24 hours.
        payload.ActiveSessionsLast24Hours.Should().Be(4);
        // First tenant created 10 days ago plus DatabaseSeeder's Tenant1 created at test setup (well within 30 days);
        // the second tenant was seeded 40 days ago which falls outside the window.
        payload.NewTenantsInPeriod.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenComputingMrrDelta_ShouldDeriveFromMrrNotSignupCount()
    {
        // three signups within the last 30 days, only one of which is paying. Two paid
        // subscriptions exist that pre-date the period start (created 60+ days ago). The MRR delta
        // must reflect the MRR added by the new paid subscription (49.99 / 200 = +25%), not the
        // signup-count ratio (3/0 → null / divide-by-zero or 3/something).
        // Arrange
        var now = DateTimeOffset.UtcNow;

        var oldPremiumSubscriptionId = SubscriptionId.NewId();
        var oldPremium = SeedTenant("Old Premium", SubscriptionPlan.Premium, now.AddDays(-90));
        SeedPaidSubscription(oldPremium, SubscriptionPlan.Premium, 100m, false, null, null, now.AddDays(-90), oldPremiumSubscriptionId);
        SeedSubscriptionCreatedEvent(oldPremium, oldPremiumSubscriptionId, 100m, now.AddDays(-90));

        var oldStandardSubscriptionId = SubscriptionId.NewId();
        var oldStandard = SeedTenant("Old Standard", SubscriptionPlan.Standard, now.AddDays(-90));
        SeedPaidSubscription(oldStandard, SubscriptionPlan.Standard, 100m, false, null, null, now.AddDays(-90), oldStandardSubscriptionId);
        SeedSubscriptionCreatedEvent(oldStandard, oldStandardSubscriptionId, 100m, now.AddDays(-90));

        // New paid subscription within the period contributes 49.99 to MRR.
        var newPaidSubscriptionId = SubscriptionId.NewId();
        var newPaid = SeedTenant("New Paid", SubscriptionPlan.Standard, now.AddDays(-5));
        SeedPaidSubscription(newPaid, SubscriptionPlan.Standard, 49.99m, false, null, null, now.AddDays(-5), newPaidSubscriptionId);
        SeedSubscriptionCreatedEvent(newPaid, newPaidSubscriptionId, 49.99m, now.AddDays(-5));

        // Two new free signups within the period contribute 0 to MRR — these would inflate a
        // signup-count delta but must not affect the MRR delta.
        SeedTenant("New Free 1", SubscriptionPlan.Basis, now.AddDays(-3));
        SeedTenant("New Free 2", SubscriptionPlan.Basis, now.AddDays(-2));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        payload.BlendedMonthlyRecurringRevenue.Should().Be(249.99m);
        // Start-of-window MRR reconstructed from the BillingEvent log: only the two -90d subscriptions
        // had SubscriptionCreated events before startOfWindow (now - 29d), so the baseline is 100 + 100 = 200.
        // End MRR includes the newPaid subscription: 200 + 49.99 = 249.99.
        // Delta: (249.99 - 200) / 200 = 24.995% → rounded to 25.0%.
        payload.BlendedMonthlyRecurringRevenueDeltaPercent.Should().Be(25.0m);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenSubscriptionsAreCancellingOrDowngrading_ShouldUseForwardMrr()
    {
        // three paid subscriptions: one stable Premium, one Premium scheduled to downgrade to Standard,
        // one Standard cancelling at period end. Forward MRR sums to 299 + 149 + 0 = 448.
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var stable = SeedTenant("Stable Premium", SubscriptionPlan.Premium, now.AddDays(-30));
        SeedPaidSubscription(stable, SubscriptionPlan.Premium, 299m, false, null, null);

        var downgrading = SeedTenant("Downgrading", SubscriptionPlan.Premium, now.AddDays(-30));
        SeedPaidSubscription(downgrading, SubscriptionPlan.Premium, 299m, false, SubscriptionPlan.Standard, 149m);

        var cancelling = SeedTenant("Cancelling", SubscriptionPlan.Standard, now.AddDays(-30));
        SeedPaidSubscription(cancelling, SubscriptionPlan.Standard, 149m, true, null, null);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        payload.BlendedMonthlyRecurringRevenue.Should().Be(448m);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenSoftDeletedTenantsExist_ShouldExcludeThemFromCounts()
    {
        // one active tenant and one soft-deleted tenant. The dashboard query bypasses the
        // tenant filter (it is cross-tenant by design) but must still respect the soft-delete filter
        // so deleted tenants do not inflate KPI counts.
        // Arrange
        var now = DateTimeOffset.UtcNow;
        SeedTenant("Active Inc", SubscriptionPlan.Standard, now.AddDays(-10));
        var deletedTenant = SeedTenant("Deleted Co", SubscriptionPlan.Standard, now.AddDays(-15));
        Connection.Update("tenants", "id", deletedTenant.Value, [("deleted_at", now.AddDays(-1))]);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder.Tenant1 plus the active tenant seeded here. The soft-deleted tenant must not count.
        payload.TotalTenants.Should().Be(2);
    }

    [Fact]
    public async Task Handle_BlendedMrrSumsAcrossActiveSubscriptionsRegardlessOfTenantSoftDelete()
    {
        // BLENDED MRR is the sum of every active subscription, regardless of tenant soft-delete state.
        // Subscription rows are immutable historical money facts that outlive the tenant lifecycle, so a
        // paid subscription on a soft-deleted tenant must still contribute. Tenant counts (Total/Active/...)
        // continue to exclude soft-deleted tenants — a deleted tenant is no longer a tenant.
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var activeTenant = SeedTenant("Active Inc", SubscriptionPlan.Standard, now.AddDays(-60));
        SeedPaidSubscription(activeTenant, SubscriptionPlan.Standard, 49.99m, false, null, null);

        var softDeletedTenant = SeedTenant("Churned Co", SubscriptionPlan.Premium, now.AddDays(-90));
        SeedPaidSubscription(softDeletedTenant, SubscriptionPlan.Premium, 99m, false, null, null, now.AddDays(-90));
        Connection.Update("tenants", "id", softDeletedTenant.Value, [("deleted_at", now.AddDays(-1))]);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardKpisResponse>();
        payload.Should().NotBeNull();
        // Both subscriptions contribute: 49.99 from the active tenant plus 99 from the soft-deleted tenant.
        payload.BlendedMonthlyRecurringRevenue.Should().Be(148.99m, "subscriptions on soft-deleted tenants must still contribute to BLENDED MRR");
        // DatabaseSeeder.Tenant1 plus the active tenant — the soft-deleted tenant is excluded from the count.
        payload.TotalTenants.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private TenantId SeedTenant(string name, SubscriptionPlan plan, DateTimeOffset createdAt)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", createdAt),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", plan.ToString()),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private void SeedPaidSubscription(
        TenantId tenantId,
        SubscriptionPlan plan,
        decimal currentPriceAmount,
        bool cancelAtPeriodEnd,
        SubscriptionPlan? scheduledPlan,
        decimal? scheduledPriceAmount,
        DateTimeOffset? createdAt = null,
        SubscriptionId? subscriptionId = null
    )
    {
        var subscriptionCreatedAt = createdAt ?? DateTimeOffset.UtcNow.AddDays(-30);
        var paymentTransactionsJson = JsonSerializer.Serialize(new[]
            {
                new PaymentTransaction(PaymentTransactionId.NewId(), currentPriceAmount, currentPriceAmount, 0m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, subscriptionCreatedAt, null, null, null, plan)
            }
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", (subscriptionId ?? SubscriptionId.NewId()).ToString()),
                ("created_at", subscriptionCreatedAt),
                ("modified_at", null),
                ("plan", plan.ToString()),
                ("scheduled_plan", scheduledPlan?.ToString()),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", currentPriceAmount),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", cancelAtPeriodEnd),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactionsJson),
                ("payment_method", null),
                ("billing_info", null),
                ("scheduled_price_amount", (object?)scheduledPriceAmount ?? DBNull.Value),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }

    private void SeedSubscriptionCreatedEvent(TenantId tenantId, SubscriptionId subscriptionId, decimal newAmount, DateTimeOffset occurredAt)
    {
        Connection.Insert("billing_events", [
                ("tenant_id", tenantId.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", subscriptionId.ToString()),
                ("created_at", occurredAt),
                ("modified_at", null),
                ("stripe_event_id", $"evt_test_{Guid.NewGuid():N}"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Standard)),
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
    }

    private void SeedSubscription(TenantId tenantId, SubscriptionPlan plan, decimal? currentPriceAmount, bool hasSucceededPayment)
    {
        var paymentTransactionsJson = hasSucceededPayment
            ? JsonSerializer.Serialize(new[]
                {
                    new PaymentTransaction(PaymentTransactionId.NewId(), 49.99m, 49.99m, 0m, MockStripeClient.MockStandardCurrency, PaymentTransactionStatus.Succeeded, DateTimeOffset.UtcNow.AddDays(-30), null, null, null, SubscriptionPlan.Standard)
                }
            )
            : "[]";

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("plan", plan.ToString()),
                ("scheduled_plan", null),
                ("stripe_customer_id", currentPriceAmount is null ? null : "cus_test"),
                ("stripe_subscription_id", currentPriceAmount is null ? null : "sub_test"),
                ("current_price_amount", (object?)currentPriceAmount ?? DBNull.Value),
                ("current_price_currency", currentPriceAmount is null ? null : MockStripeClient.MockStandardCurrency),
                ("current_period_end", currentPriceAmount is null ? null : DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactionsJson),
                ("payment_method", null),
                ("billing_info", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }

    private UserId SeedUser(TenantId tenantId, string email, DateTimeOffset createdAt)
    {
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", userId.ToString()),
                ("created_at", createdAt),
                ("modified_at", null),
                ("email", email),
                ("external_identities", "[]"),
                ("email_confirmed", true),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", nameof(UserRole.Owner)),
                ("locale", "en-US"),
                ("avatar", JsonSerializer.Serialize(new Avatar()))
            ]
        );
        return userId;
    }

    private void SeedSession(TenantId tenantId, UserId userId, DateTimeOffset createdAt, bool revoked)
    {
        Connection.Insert("sessions", [
                ("tenant_id", tenantId.Value),
                ("id", SessionId.NewId().ToString()),
                ("user_id", userId.ToString()),
                ("created_at", createdAt),
                ("modified_at", null),
                ("refresh_token_jti", RefreshTokenJti.NewId().ToString()),
                ("previous_refresh_token_jti", null),
                ("refresh_token_version", 1),
                ("login_method", nameof(LoginMethod.OneTimePassword)),
                ("device_type", nameof(DeviceType.Desktop)),
                ("user_agent", "Mozilla/5.0"),
                ("ip_address", "127.0.0.1"),
                ("revoked_at", revoked ? createdAt.AddMinutes(5) : null),
                ("revoked_reason", revoked ? nameof(SessionRevokedReason.LoggedOut) : null)
            ]
        );
    }
}
