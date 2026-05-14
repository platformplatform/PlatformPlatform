using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.ExternalAuthentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardTrendsTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetDashboardTrends_WhenMetricIsNewTenants_ShouldBucketTenantsByCreatedDate()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        SeedTenant("Today Inc", now);
        SeedTenant("Yesterday Inc", now.AddDays(-1));
        SeedTenant("Three Days Ago Inc", now.AddDays(-3));
        SeedTenant("Outside Period Inc", now.AddDays(-20));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=NewTenants&period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardTrendsResponse>();
        payload.Should().NotBeNull();
        payload.Metric.Should().Be(DashboardTrendMetric.NewTenants);
        payload.Period.Should().Be(DashboardTrendPeriod.Last7Days);
        payload.Points.Should().HaveCount(7);
        // DatabaseSeeder's Tenant1 is created today during test setup, so it appears in the today bucket.
        payload.Points.Sum(p => p.Value).Should().Be(4);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        payload.Points.Single(p => p.Date == today).Value.Should().Be(2);
        payload.Points.Single(p => p.Date == today.AddDays(-1)).Value.Should().Be(1);
        payload.Points.Single(p => p.Date == today.AddDays(-3)).Value.Should().Be(1);
        // Prior period covers the 7 days immediately before the current window — same length, all dates strictly older.
        payload.PriorPoints.Should().HaveCount(7);
        payload.PriorPoints.Should().OnlyContain(p => p.Date < payload.Points[0].Date);
        payload.PriorPoints.Should().OnlyContain(p => p.Value == 0);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenMetricIsNewUsers_ShouldBucketUsersByCreatedDate()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tenant = SeedTenant("Trend Tenant", now.AddDays(-60));
        SeedUser(tenant, "today@example.com", now);
        SeedUser(tenant, "ten-days-ago@example.com", now.AddDays(-10));
        SeedUser(tenant, "outside@example.com", now.AddDays(-45));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=NewUsers&period=Last30Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardTrendsResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(30);
        // DatabaseSeeder seeds 2 users in Tenant1 today; combined with the 2 users seeded above within 30 days.
        payload.Points.Sum(p => p.Value).Should().Be(4);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        payload.Points.Single(p => p.Date == today.AddDays(-10)).Value.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenMetricIsLoginActivity_ShouldSumEmailAndExternalLogins()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        SeedEmailLogin("user1@example.com", true, now);
        SeedEmailLogin("user2@example.com", true, now.AddDays(-2));
        SeedEmailLogin("user3@example.com", false, now);
        SeedExternalLogin("user4@example.com", ExternalLoginResult.Success, now);
        SeedExternalLogin("user5@example.com", ExternalLoginResult.Success, now.AddDays(-2));
        SeedExternalLogin("user6@example.com", ExternalLoginResult.IdentityProviderError, now);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=LoginActivity&period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardTrendsResponse>();
        payload.Should().NotBeNull();
        payload.Points.Sum(p => p.Value).Should().Be(4);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        payload.Points.Single(p => p.Date == today).Value.Should().Be(2);
        payload.Points.Single(p => p.Date == today.AddDays(-2)).Value.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenNoEventsInPeriod_ShouldReturnZeroFilledPoints()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=LoginActivity&period=Last90Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardTrendsResponse>();
        payload.Should().NotBeNull();
        payload.Points.Should().HaveCount(90);
        payload.Points.Should().OnlyContain(p => p.Value == 0);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenMetricIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenPeriodIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=NewTenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=NewTenants&period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardTrends_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/trends?metric=NewTenants&period=Last7Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private TenantId SeedTenant(string name, DateTimeOffset createdAt)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", createdAt),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("rollout_bucket", 50)
            ]
        );
        return tenantId;
    }

    private void SeedUser(TenantId tenantId, string email, DateTimeOffset createdAt)
    {
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", UserId.NewId().ToString()),
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
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("rollout_bucket", 50)
            ]
        );
    }

    private void SeedEmailLogin(string email, bool completed, DateTimeOffset createdAt)
    {
        Connection.Insert("email_logins", [
                ("id", EmailLoginId.NewId().ToString()),
                ("created_at", createdAt),
                ("modified_at", null),
                ("type", nameof(EmailLoginType.Login)),
                ("email", email.ToLower()),
                ("one_time_password_hash", "hash"),
                ("retry_count", 0),
                ("resend_count", 0),
                ("completed", completed)
            ]
        );
    }

    private void SeedExternalLogin(string email, ExternalLoginResult result, DateTimeOffset createdAt)
    {
        Connection.Insert("external_logins", [
                ("id", ExternalLoginId.NewId().ToString()),
                ("created_at", createdAt),
                ("modified_at", null),
                ("type", nameof(ExternalLoginType.Login)),
                ("provider_type", nameof(ExternalProviderType.Google)),
                ("email", email.ToLower()),
                ("code_verifier", "code-verifier"),
                ("nonce", "nonce"),
                ("browser_fingerprint", "fingerprint"),
                ("login_result", result.ToString())
            ]
        );
    }
}
