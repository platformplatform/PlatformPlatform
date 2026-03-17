using System.Net;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class DeleteTenantTests : EndpointBaseTest<AccountDbContext>
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
