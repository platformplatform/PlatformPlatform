using FluentAssertions;
using NUlid;
using PlatformPlatform.SharedKernel.DomainCore.Identity;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.DomainCore.Identity;

public class StronglyTypedUlidTests
{
    [Fact]
    public void NewId_WhenGeneratingIdWithPrefix_IdShouldContainThePrefix()
    {
        // Arrange
        // Act
        var id = IdWithPrefix.NewId();

        // Assert
        id.Value.Should().StartWith("prefix_");
    }

    [Fact]
    public void NewId_WhenGeneratingIdWithoutPrefix_IdShouldNotContainAnyPrefix()
    {
        // Arrange
        // Act
        var id = IdWithEmptyPrefix.NewId();

        // Assert
        id.Value.Should().NotContain("_");
        Ulid.TryParse(id, out _).Should().BeTrue();
    }

    [Fact]
    public void TryParse_WhenParsingIdWithPrefix_IdBeParsedSuccessfully()
    {
        // Arrange
        var id = IdWithPrefix.NewId();

        // Act
        var isParsedSuccessfully = IdWithPrefix.TryParse(id, out var result);

        // Assert
        isParsedSuccessfully.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public void TryParse_WhenParsingIdWithoutPrefix_IdBeParsedSuccessfully()
    {
        // Arrange
        var id = IdWithEmptyPrefix.NewId();

        // Act
        var isParsedSuccessfully = IdWithPrefix.TryParse(id, out var result);

        // Assert
        isParsedSuccessfully.Should().BeTrue();
        result.Should().NotBeNull();
    }


    [IdPrefix("prefix")]
    [UsedImplicitly]
    public record IdWithPrefix(string Value) : StronglyTypedUlid<IdWithPrefix>(Value);

    [UsedImplicitly]
    [IdPrefix("")]
    public record IdWithEmptyPrefix(string Value) : StronglyTypedUlid<IdWithEmptyPrefix>(Value);
}