using System.Net;
using System.Net.Http.Json;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardRecentSignupsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardRecentSignups_WhenCalled_ShouldReturnMostRecentTenantsFirst()
    {
        // three tenants seeded into the far future so they sort newer than any tenant DatabaseSeeder
        // creates at test setup. The response should list them in reverse chronological order.
        // Arrange
        var future = DateTimeOffset.UtcNow.AddYears(1);
        SeedTenant("Newest", future);
        SeedTenant("Middle", future.AddDays(-1));
        SeedTenant("Oldest", future.AddDays(-2));

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-signups?Limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRecentSignupsResponse>();
        payload.Should().NotBeNull();
        var names = payload.Signups.Select(s => s.Name).ToArray();
        // The seeded tenants are dated one year in the future, so they must appear before any tenant DatabaseSeeder
        // creates at test setup time. Combined assertion + diagnostic message: include the actual names in the
        // failure message so future regressions surface the real ordering.
        names.Should().Contain("Newest", $"actual order: [{string.Join(", ", names)}]");
        names.Should().Contain("Middle");
        names.Should().Contain("Oldest");
        Array.IndexOf(names, "Newest").Should().BeLessThan(Array.IndexOf(names, "Middle"));
        Array.IndexOf(names, "Middle").Should().BeLessThan(Array.IndexOf(names, "Oldest"));
    }

    [Fact]
    public async Task GetDashboardRecentSignups_WhenCalledWithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-signups?Limit=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardRecentSignups_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-signups?Limit=6");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void SeedTenant(string name, DateTimeOffset createdAt)
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
    }
}
