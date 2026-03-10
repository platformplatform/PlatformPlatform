using System.Net;
using Account.Database;
using FluentAssertions;
using Xunit;

namespace Account.Tests.Electric;

public sealed class ElectricShapeProxyTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ProxyShapeRequest_WhenFeatureFlagsTableRequested_ShouldAcceptTable()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/electric/v1/shape?table=feature_flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "table is registered but ELECTRIC_URL is not configured in tests");
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenUsersTableRequested_ShouldAcceptTable()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/electric/v1/shape?table=users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "table is registered but ELECTRIC_URL is not configured in tests");
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenUnknownTableRequested_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/electric/v1/shape?table=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenNoTableSpecified_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/electric/v1/shape");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account/electric/v1/shape?table=feature_flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenSubscriptionsRequestedByMember_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/electric/v1/shape?table=subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProxyShapeRequest_WhenFeatureFlagsRequestedByMember_ShouldAcceptTable()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/electric/v1/shape?table=feature_flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "feature_flags has no RequiredRole so all authenticated users can access it");
    }
}
