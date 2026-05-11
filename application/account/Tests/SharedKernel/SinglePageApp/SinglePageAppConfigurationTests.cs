using FluentAssertions;
using SharedKernel.SinglePageApp;
using Xunit;

namespace Account.Tests.SharedKernel.SinglePageApp;

public sealed class SinglePageAppConfigurationTests
{
    [Fact]
    public void Constructor_WhenRunningInAzure_ShouldNotThrowAndShouldLeavePublicDirectoryNull()
    {
        // Act
        var act = () => new SinglePageAppConfiguration(
            false, null, "BackOffice", "https://example.com", "https://example.com", true
        );

        // Assert
        var configuration = act.Should().NotThrow().Subject;
        configuration.PublicDirectory.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenGivenRuntimeFeatureFlags_ShouldEmbedThemInStaticRuntimeEnvironment()
    {
        // Arrange
        // Mirrors the dictionary built in account/Api/Program.cs so a regression there (e.g. the back-office
        // ctor not getting the dictionary plumbed in) surfaces here as well as in the SPA-shell HTML.
        var environmentVariables = new Dictionary<string, string>
        {
            ["PUBLIC_GOOGLE_OAUTH_ENABLED"] = "false",
            ["PUBLIC_SUBSCRIPTION_ENABLED"] = "false"
        };

        // Act
        var configuration = new SinglePageAppConfiguration(
            false, environmentVariables, "BackOffice", "https://example.com", "https://example.com", true
        );

        // Assert
        configuration.StaticRuntimeEnvironment.Should().Contain("PUBLIC_GOOGLE_OAUTH_ENABLED", "false");
        configuration.StaticRuntimeEnvironment.Should().Contain("PUBLIC_SUBSCRIPTION_ENABLED", "false");
    }
}
