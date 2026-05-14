using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.FeatureFlags.Commands;
using Account.Features.FeatureFlags.Queries;
using FluentAssertions;
using SharedKernel.Authentication;
using SharedKernel.FeatureFlags;
using SharedKernel.Tests;
using Xunit;

namespace Account.Tests.FeatureFlags;

// Tests for the owner/user-facing /api/account/feature-flags/* endpoints plus pure unit tests for the
// rollout-bucket math. Back-office (cross-tenant) feature-flag behavior is exercised in the parallel
// FeatureFlagBackOfficeTests.cs under Tests/BackOffice/FeatureFlags/ — that file owns the kill-switch,
// rollout, override, and listing flows that used to live on /internal-api/account/feature-flags/*.
public sealed class FeatureFlagTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    // Tenant override tests (owner API)

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForConfigurableFlag_ShouldSucceed()
    {
        // Arrange
        var flagKey = "account-overview";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagTenantOverrideSet");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Owner");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForNonConfigurableFlag_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "sso";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{flagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenOwnerForAdminOnlyFlag_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "beta-features";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"Feature flag '{flagKey}' is not configurable by tenant owners.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var flagKey = "account-overview";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to configure tenant feature flags.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    // User override tests (owner API)

    [Fact]
    public async Task SetUserFeatureFlag_WhenUserConfigurable_ShouldCreateOverrideRow()
    {
        // Arrange
        var flagKey = "compact-view";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("FeatureFlagUserOverrideSet");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.trigger"].Should().Be("Self");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserFeatureFlag_WhenNotUserScoped_ShouldFailValidation()
    {
        // Arrange
        var flagKey = "sso";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    // Configurable-flag query tests

    [Fact]
    public async Task GetTenantConfigurableFlags_WhenCalled_ShouldReturnConfigurableFlagsWithCurrentOverrideState()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/feature-flags/tenant-configurable");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<TenantConfigurableFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().Contain(f => f.FlagKey == "account-overview" && f.Enabled == false);
    }

    [Fact]
    public async Task GetUserConfigurableFlags_WhenCalled_ShouldReturnConfigurableUserFlagsWithCurrentOverrideState()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/feature-flags/user-configurable");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.DeserializeResponse<UserConfigurableFeatureFlagsResponse>();
        result.Should().NotBeNull();
        result.Flags.Should().Contain(f => f.FlagKey == "compact-view" && f.Enabled == false);
    }

    // Self-service flag mutations chain AddRefreshAuthenticationTokens() so the actor's own JWT is
    // refreshed by the gateway in the same request cycle. Back-office mutations do not.

    [Fact]
    public async Task SetTenantFeatureFlagOwner_WhenCalled_ShouldAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "account-overview";
        var command = new SetTenantFeatureFlagOwnerCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/tenant-override", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    [Fact]
    public async Task SetUserFeatureFlag_WhenCalled_ShouldAddRefreshAuthenticationTokensHeader()
    {
        // Arrange
        var flagKey = "compact-view";
        var command = new SetUserFeatureFlagCommand { Enabled = true };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/feature-flags/{flagKey}/user-override", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    // A/B rollout bucket math (pure unit tests, no HTTP)

    [Fact]
    public void BucketRange_WhenNormalRange_ShouldMatchCorrectly()
    {
        IsInRolloutBucketRange(50, 40, 60).Should().BeTrue();
        IsInRolloutBucketRange(39, 40, 60).Should().BeFalse();
        IsInRolloutBucketRange(61, 40, 60).Should().BeFalse();
        IsInRolloutBucketRange(40, 40, 60).Should().BeTrue();
        IsInRolloutBucketRange(60, 40, 60).Should().BeTrue();
    }

    [Fact]
    public void BucketRange_WhenWrapAround_ShouldMatchCorrectly()
    {
        // Wrap-around within 0-99 range.
        IsInRolloutBucketRange(95, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(5, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(50, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(90, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(10, 90, 10).Should().BeTrue();
        IsInRolloutBucketRange(11, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(89, 90, 10).Should().BeFalse();
        IsInRolloutBucketRange(0, 90, 10).Should().BeTrue();
    }

    [Fact]
    public void RolloutBucket_ShouldBeDeterministic()
    {
        // Arrange
        var sequenceNumber = 42;

        // Act
        var bucket1 = RolloutBucketHasher.ComputeRolloutBucket(sequenceNumber);
        var bucket2 = RolloutBucketHasher.ComputeRolloutBucket(sequenceNumber);

        // Assert
        bucket1.Should().Be(bucket2);
        bucket1.Should().BeInRange(0, 99);
    }

    [Fact]
    public void VanDerCorput_ShouldDistributeEvenly()
    {
        // Arrange
        var bucketCounts = new int[100];

        // Act
        for (var i = 0; i < 1000; i++)
        {
            var bucket = RolloutBucketHasher.ComputeRolloutBucket(i);
            bucket.Should().BeInRange(0, 99);
            bucketCounts[bucket]++;
        }

        // Assert
        foreach (var count in bucketCounts)
        {
            count.Should().BeInRange(9, 11, "van der Corput should distribute within +/-1 of ideal");
        }
    }

    private static bool IsInRolloutBucketRange(int bucket, int rolloutBucketStart, int rolloutBucketEnd)
    {
        return RolloutBucketHasher.IsInRolloutBucketRange(bucket, rolloutBucketStart, rolloutBucketEnd);
    }
}
