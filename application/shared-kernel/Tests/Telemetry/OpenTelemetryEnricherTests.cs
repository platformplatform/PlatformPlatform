using System.Net;
using FluentAssertions;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;
using Xunit;

namespace SharedKernel.Tests.Telemetry;

public sealed class OpenTelemetryEnricherTests
{
    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> options)
    {
        return ActivitySamplingResult.AllData;
    }

    [Fact]
    public void Apply_WhenUserIsAuthenticated_ShouldSetSessionIdTag()
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

        var enricher = new OpenTelemetryEnricher(executionContext);

        using var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        using (listener)
        {
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity();
            activity.Should().NotBeNull();

            // Act
            enricher.Apply();

            // Assert
            var sessionIdTag = activity.Tags.FirstOrDefault(t => t.Key == "user.session_id");
            sessionIdTag.Value.Should().Be(sessionId.Value);

            var subscriptionPlanTag = activity.Tags.FirstOrDefault(t => t.Key == "tenant.subscription_plan");
            subscriptionPlanTag.Value.Should().Be("Standard");

            var zoomLevelTag = activity.Tags.FirstOrDefault(t => t.Key == "user.zoom_level");
            zoomLevelTag.Value.Should().Be("1.25");

            var themeTag = activity.Tags.FirstOrDefault(t => t.Key == "user.theme");
            themeTag.Value.Should().Be("dark");
        }
    }

    [Fact]
    public void Apply_WhenSessionIdIsNull_ShouldSetNullSessionIdTag()
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

        var enricher = new OpenTelemetryEnricher(executionContext);

        using var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        using (listener)
        {
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity();
            activity.Should().NotBeNull();

            // Act
            enricher.Apply();

            // Assert
            var sessionIdTag = activity.Tags.FirstOrDefault(t => t.Key == "user.session_id");
            sessionIdTag.Value.Should().BeNull();

            activity.Tags.Should().NotContain(t => t.Key == "user.zoom_level");
            activity.Tags.Should().NotContain(t => t.Key == "user.theme");
        }
    }

    [Fact]
    public void Apply_WhenTrackableFeatureFlagsEnabled_ShouldEmitScopedPerFlagTags()
    {
        // Arrange - beta-features (Tenant) and experimental-ui (User) are the registry's
        // two TrackInTelemetry=true flags with different scopes.
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

        var enricher = new OpenTelemetryEnricher(executionContext);

        using var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener { ShouldListenTo = _ => true, Sample = SampleAllData };
        using (listener)
        {
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity();
            activity.Should().NotBeNull();

            // Act
            enricher.Apply();

            // Assert - per-flag tags scoped by who carries the setting, value is "enabled",
            // and the OTel-reserved feature_flag.* namespace is never emitted.
            activity.Tags.Should().Contain(t => t.Key == "tenant.feature_flags.beta-features" && t.Value == "enabled");
            activity.Tags.Should().Contain(t => t.Key == "user.feature_flags.experimental-ui" && t.Value == "enabled");
            activity.Tags.Should().NotContain(t => t.Key.StartsWith("feature_flag.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Apply_WhenNoTrackableFeatureFlagsEnabled_ShouldOmitFeatureFlagsTag()
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

        var enricher = new OpenTelemetryEnricher(executionContext);

        using var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener { ShouldListenTo = _ => true, Sample = SampleAllData };
        using (listener)
        {
            ActivitySource.AddActivityListener(listener);

            using var activity = activitySource.StartActivity();
            activity.Should().NotBeNull();

            // Act
            enricher.Apply();

            // Assert
            activity.Tags.Should().NotContain(t => t.Key.StartsWith("user.feature_flags.", StringComparison.Ordinal));
            activity.Tags.Should().NotContain(t => t.Key.StartsWith("tenant.feature_flags.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Apply_WhenNoCurrentActivity_ShouldNotThrow()
    {
        // Arrange
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = UserId.NewId(),
            SessionId = SessionId.NewId(),
            Locale = "en-US",
            Role = "Admin"
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns((TenantId?)null);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        var enricher = new OpenTelemetryEnricher(executionContext);

        // Act & Assert - Should not throw when Activity.Current is null
        var act = enricher.Apply;
        act.Should().NotThrow();
    }
}
