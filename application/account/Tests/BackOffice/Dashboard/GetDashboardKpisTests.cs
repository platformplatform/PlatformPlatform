using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Authentication.Domain;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
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
        payload.Currency.Should().Be("DKK");
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

    private void SeedSubscription(TenantId tenantId, SubscriptionPlan plan, decimal? currentPriceAmount, bool hasSucceededPayment)
    {
        var paymentTransactionsJson = hasSucceededPayment
            ? JsonSerializer.Serialize(new[]
                {
                    new PaymentTransaction(PaymentTransactionId.NewId(), 49.99m, "DKK", PaymentTransactionStatus.Succeeded, DateTimeOffset.UtcNow.AddDays(-30), null, null, null, SubscriptionPlan.Standard)
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
                ("current_price_currency", currentPriceAmount is null ? null : "DKK"),
                ("current_period_end", currentPriceAmount is null ? null : DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactionsJson),
                ("payment_method", null),
                ("billing_info", null)
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
