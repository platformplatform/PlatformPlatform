using FluentAssertions;
using PlatformPlatform.SharedKernel.StronglyTypedIds;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.StronglyTypedIds;

public class StronglyTypedUlidTests
{
    [Fact]
    public void NewId_WhenGenerating_ShouldHavePrefix()
    {
        // Arrange
        // Act
        var id = IdWithPrefix.NewId();

        // Assert
        id.Value.Should().StartWith("prefix_");
    }

    [Fact]
    public void TryParse_WhenValidId_ShouldSucceed()
    {
        // Arrange
        var id = IdWithPrefix.NewId();

        // Act
        var isParsedSuccessfully = IdWithPrefix.TryParse(id, out var result);

        // Assert
        isParsedSuccessfully.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [IdPrefix("prefix")]
    public record IdWithPrefix(string Value) : StronglyTypedUlid<IdWithPrefix>(Value);
}
