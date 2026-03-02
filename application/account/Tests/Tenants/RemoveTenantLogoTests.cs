using System.Net;
using Account.Database;
using Account.Features;
using FluentAssertions;
using SharedKernel.Tests;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class RemoveTenantLogoTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task RemoveTenantLogo_WhenOwnerUser_ShouldSucceed()
    {
        // Arrange
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync("/api/account/tenants/current/remove-logo");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Should().BeOfType<TenantLogoRemoved>();
    }

    [Fact]
    public async Task RemoveTenantLogo_WhenMemberUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.DeleteAsync("/api/account/tenants/current/remove-logo");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to remove tenant logo.");
    }
}
