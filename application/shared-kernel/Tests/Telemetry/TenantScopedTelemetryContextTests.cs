using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Telemetry;

public sealed class TenantScopedTelemetryContextTests
{
    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> options)
    {
        return ActivitySamplingResult.AllData;
    }

    [Fact]
    public void Set_WhenCalledWithTenantData_ShouldSetTelemetryProperties()
    {
        // Arrange
        var tenantId = new TenantId(99999);

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
            TenantScopedTelemetryContext.Set(tenantId, "Premium");

            // Assert - OpenTelemetry Activity tags
            var tenantIdTag = activity.TagObjects.FirstOrDefault(t => t.Key == "tenant.id");
            tenantIdTag.Value.Should().Be(99999L);

            var subscriptionPlanTag = activity.Tags.FirstOrDefault(t => t.Key == "tenant.subscription_plan");
            subscriptionPlanTag.Value.Should().Be("Premium");

            // Assert - Application Insights properties
            var telemetry = new RequestTelemetry();
            var initializer = new ApplicationInsightsTelemetryInitializer();
            initializer.Initialize(telemetry);

            telemetry.Context.User.AccountId.Should().Be("99999");
            telemetry.Context.GlobalProperties["tenant.id"].Should().Be("99999");
            telemetry.Context.GlobalProperties["tenant.subscription_plan"].Should().Be("Premium");
            telemetry.Context.GlobalProperties.Should().NotContainKey("user.session_id");
            telemetry.Context.User.AuthenticatedUserId.Should().BeNull();
        }
    }
}
