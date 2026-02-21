using System.Net;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using NSubstitute;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Telemetry;

public sealed class ApplicationInsightsTelemetryInitializerTests
{
    [Fact]
    public void Initialize_WhenUserIsAuthenticated_ShouldSetSessionIdInTelemetry()
    {
        // Arrange
        var sessionId = SessionId.NewId();
        var userId = UserId.NewId();
        var tenantId = new TenantId(12345);
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = userId,
            TenantId = tenantId,
            SubscriptionPlan = "Standard",
            SessionId = sessionId,
            Locale = "en-US",
            ZoomLevel = "1.25",
            Theme = "dark",
            Role = "Admin",
            IsInternalUser = false
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns(tenantId);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        var telemetry = new RequestTelemetry();
        var initializer = new ApplicationInsightsTelemetryInitializer();

        // Act
        initializer.Initialize(telemetry);

        // Assert
        telemetry.Context.GlobalProperties.Should().ContainKey("user.session_id");
        telemetry.Context.GlobalProperties["user.session_id"].Should().Be(sessionId.Value);

        telemetry.Context.GlobalProperties.Should().ContainKey("tenant.subscription_plan");
        telemetry.Context.GlobalProperties["tenant.subscription_plan"].Should().Be("Standard");

        telemetry.Context.GlobalProperties.Should().ContainKey("user.zoom_level");
        telemetry.Context.GlobalProperties["user.zoom_level"].Should().Be("1.25");

        telemetry.Context.GlobalProperties.Should().ContainKey("user.theme");
        telemetry.Context.GlobalProperties["user.theme"].Should().Be("dark");
    }

    [Fact]
    public void Initialize_WhenUserIsNotAuthenticated_ShouldNotSetSessionIdInTelemetry()
    {
        // Arrange
        var userInfo = new UserInfo
        {
            IsAuthenticated = false,
            Locale = "en-US",
            IsInternalUser = false
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns((TenantId?)null);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        var telemetry = new RequestTelemetry();
        var initializer = new ApplicationInsightsTelemetryInitializer();

        // Act
        initializer.Initialize(telemetry);

        // Assert
        telemetry.Context.GlobalProperties.Should().NotContainKey("user.session_id");
        telemetry.Context.GlobalProperties.Should().NotContainKey("tenant.subscription_plan");
        telemetry.Context.GlobalProperties.Should().NotContainKey("user.zoom_level");
        telemetry.Context.GlobalProperties.Should().NotContainKey("user.theme");
    }

    [Fact]
    public void Initialize_WhenSessionIdIsNull_ShouldNotSetSessionIdInTelemetry()
    {
        // Arrange
        var userId = UserId.NewId();
        var tenantId = new TenantId(12345);
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = userId,
            TenantId = tenantId,
            SessionId = null,
            Locale = "en-US",
            Role = "Admin",
            IsInternalUser = false
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns(tenantId);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        var telemetry = new RequestTelemetry();
        var initializer = new ApplicationInsightsTelemetryInitializer();

        // Act
        initializer.Initialize(telemetry);

        // Assert
        telemetry.Context.GlobalProperties.Should().NotContainKey("user.session_id");
    }
}
