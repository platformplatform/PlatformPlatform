using System.Net;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Telemetry;

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
            Role = "Admin",
            IsInternalUser = false
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
            Role = "Admin",
            IsInternalUser = false
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
    public void Apply_WhenNoCurrentActivity_ShouldNotThrow()
    {
        // Arrange
        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            Id = UserId.NewId(),
            SessionId = SessionId.NewId(),
            Locale = "en-US",
            Role = "Admin",
            IsInternalUser = false
        };

        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.UserInfo.Returns(userInfo);
        executionContext.TenantId.Returns((TenantId?)null);
        executionContext.ClientIpAddress.Returns(IPAddress.Parse("192.168.1.1"));

        var enricher = new OpenTelemetryEnricher(executionContext);

        // Act & Assert - Should not throw when Activity.Current is null
        var act = () => enricher.Apply();
        act.Should().NotThrow();
    }
}
