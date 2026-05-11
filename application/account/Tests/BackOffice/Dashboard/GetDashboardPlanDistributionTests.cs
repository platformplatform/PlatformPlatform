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

public sealed class GetDashboardPlanDistributionTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardPlanDistribution_WhenCalled_ShouldReturnEntryPerPlan()
    {
        // DatabaseSeeder seeds one Basis tenant; we add one Standard and one Premium.
        // Arrange
        SeedTenant("Standard Inc", SubscriptionPlan.Standard);
        SeedTenant("Premium Ltd", SubscriptionPlan.Premium);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/plan-distribution");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardPlanDistributionResponse>();
        payload.Should().NotBeNull();
        payload.TotalTenants.Should().Be(3);
        payload.Distribution.Should().HaveCount(Enum.GetValues<SubscriptionPlan>().Length);
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Basis).Count.Should().Be(1);
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Standard).Count.Should().Be(1);
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Premium).Count.Should().Be(1);
        payload.Distribution.Should().OnlyContain(entry => entry.Percentage > 0d);
    }

    [Fact]
    public async Task Handle_WhenTenantSoftDeleted_ExcludesFromCurrentDistribution()
    {
        // Plan distribution is a forward-looking current-state snapshot — a soft-deleted tenant has no
        // "current plan" by definition and must drop out. DatabaseSeeder.Tenant1 (Basis) is the only tenant
        // that remains visible alongside the active Standard tenant; the Premium tenant is soft-deleted.
        // Arrange
        SeedTenant("Active Standard", SubscriptionPlan.Standard);
        var deletedTenantId = SeedTenant("Churned Premium", SubscriptionPlan.Premium);
        Connection.Update("tenants", "id", deletedTenantId.Value, [("deleted_at", DateTimeOffset.UtcNow.AddDays(-1))]);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/plan-distribution");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardPlanDistributionResponse>();
        payload.Should().NotBeNull();
        payload.TotalTenants.Should().Be(2, "soft-deleted tenants must not count toward the current-state snapshot");
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Basis).Count.Should().Be(1);
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Standard).Count.Should().Be(1);
        payload.Distribution.Single(d => d.Plan == SubscriptionPlan.Premium).Count.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardPlanDistribution_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/plan-distribution");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private TenantId SeedTenant(string name, SubscriptionPlan plan)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", plan.ToString()),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }
}
