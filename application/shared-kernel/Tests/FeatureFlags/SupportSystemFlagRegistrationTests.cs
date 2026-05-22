using FluentAssertions;
using SharedKernel.FeatureFlags;
using Xunit;
using FeatureFlagRegistry = SharedKernel.FeatureFlags.FeatureFlags;

namespace SharedKernel.Tests.FeatureFlags;

public sealed class SupportSystemFlagRegistrationTests
{
    [Fact]
    public void SupportSystemFlag_ShouldBeRegisteredAsSystemScopeWithExpectedFrontendEnvVar()
    {
        var supportSystem = FeatureFlagRegistry.Get("support-system");

        supportSystem.Should().NotBeNull("the support-system flag must be registered so the SPA can read its runtime env var");
        supportSystem.Scope.Should().Be(FeatureFlagScope.System);
        supportSystem.FrontendEnvVar.Should().Be("PUBLIC_SUPPORT_SYSTEM_ENABLED");
        supportSystem.SystemConfigKey.Should().Be("Support:Enabled");
    }
}
