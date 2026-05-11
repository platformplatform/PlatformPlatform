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
}
