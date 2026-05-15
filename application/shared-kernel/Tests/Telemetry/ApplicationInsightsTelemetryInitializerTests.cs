using System.Net;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;
using Xunit;

namespace SharedKernel.Tests.Telemetry;

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
            Role = "Admin"
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
            Locale = "en-US"
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
    public void Initialize_WhenTrackableFeatureFlagsEnabled_ShouldEmitScopedPerFlagProperties()
    {
        // Arrange - beta-features (Tenant) and experimental-ui (User) are the registry's two
        // TrackInTelemetry=true flags with different scopes. Symmetric with OpenTelemetryEnricherTests.
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = UserId.NewId(),
            TenantId = new TenantId(12345),
            Locale = "en-US",
            Role = "Admin",
            FeatureFlags = new HashSet<string> { "experimental-ui", "beta-features" }
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns(userInfo.TenantId);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        var telemetry = new RequestTelemetry();
        var initializer = new ApplicationInsightsTelemetryInitializer();

        // Act
        initializer.Initialize(telemetry);

        // Assert - per-flag dimensions scoped by who carries the setting, value is "enabled",
        // and the OTel-reserved feature_flag.* namespace is never emitted.
        telemetry.Context.GlobalProperties.Should().ContainKey("tenant.feature_flags.beta-features");
        telemetry.Context.GlobalProperties["tenant.feature_flags.beta-features"].Should().Be("enabled");
        telemetry.Context.GlobalProperties.Should().ContainKey("user.feature_flags.experimental-ui");
        telemetry.Context.GlobalProperties["user.feature_flags.experimental-ui"].Should().Be("enabled");
        telemetry.Context.GlobalProperties.Keys.Should().NotContain(k => k.StartsWith("feature_flag.", StringComparison.Ordinal));
    }

    [Fact]
    public void Initialize_WhenNoTrackableFeatureFlagsEnabled_ShouldOmitFeatureFlagProperties()
    {
        // Arrange
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = UserId.NewId(),
            TenantId = new TenantId(12345),
            Locale = "en-US",
            Role = "Admin",
            FeatureFlags = new HashSet<string>()
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns(userInfo.TenantId);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        ApplicationInsightsTelemetryInitializer.SetContext(executionContext);

        var telemetry = new RequestTelemetry();
        var initializer = new ApplicationInsightsTelemetryInitializer();

        // Act
        initializer.Initialize(telemetry);

        // Assert
        telemetry.Context.GlobalProperties.Keys.Should().NotContain(k => k.StartsWith("user.feature_flags.", StringComparison.Ordinal));
        telemetry.Context.GlobalProperties.Keys.Should().NotContain(k => k.StartsWith("tenant.feature_flags.", StringComparison.Ordinal));
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
            Role = "Admin"
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
