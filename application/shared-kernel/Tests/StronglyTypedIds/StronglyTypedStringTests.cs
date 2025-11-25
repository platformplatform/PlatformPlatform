using FluentAssertions;
using PlatformPlatform.SharedKernel.StronglyTypedIds;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.StronglyTypedIds;

public class StronglyTypedStringTests
{
    [Fact]
    public void NewId_WhenValidPrefixedValue_ShouldAcceptAsIs()
    {
        // Arrange & Act
        var id = PrefixedStringId.NewId("cus_test123");

        // Assert
        id.Value.Should().Be("cus_test123");
    }

    [Fact]
    public void NewId_WhenPrefixMissing_ShouldThrow()
    {
        // Arrange & Act
        var act = () => PrefixedStringId.NewId("test123");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Value must start with prefix 'cus_' followed by at least one character*");
    }

    [Fact]
    public void NewId_WhenOnlyPrefix_ShouldThrow()
    {
        // Arrange & Act
        var act = () => PrefixedStringId.NewId("cus_");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Value must start with prefix 'cus_' followed by at least one character*");
    }

    [Fact]
    public void TryParse_WhenOnlyPrefix_ShouldFail()
    {
        // Arrange & Act
        var isParsedSuccessfully = PrefixedStringId.TryParse("cus_", out var result);

        // Assert
        isParsedSuccessfully.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void NewId_WhenCreatingWithoutPrefix_ShouldKeepValueAsIs()
    {
        // Arrange & Act
        var id = UnprefixedStringId.NewId("test123");

        // Assert
        id.Value.Should().Be("test123");
    }

    [Fact]
    public void TryParse_WhenValidIdWithPrefix_ShouldSucceed()
    {
        // Arrange
        var id = PrefixedStringId.NewId("cus_test123");

        // Act
        var isParsedSuccessfully = PrefixedStringId.TryParse(id.Value, out var result);

        // Assert
        isParsedSuccessfully.Should().BeTrue();
        result.Should().NotBeNull();
        result.Value.Should().Be("cus_test123");
    }

    [Fact]
    public void TryParse_WhenInvalidPrefix_ShouldFail()
    {
        // Arrange & Act
        var isParsedSuccessfully = PrefixedStringId.TryParse("wrong_test123", out var result);

        // Assert
        isParsedSuccessfully.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryParse_WhenNullValue_ShouldFail()
    {
        // Arrange & Act
        var isParsedSuccessfully = PrefixedStringId.TryParse(null, out var result);

        // Assert
        isParsedSuccessfully.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryParse_WhenUnprefixedId_ShouldSucceed()
    {
        // Arrange & Act
        var isParsedSuccessfully = UnprefixedStringId.TryParse("anyvalue", out var result);

        // Assert
        isParsedSuccessfully.Should().BeTrue();
        result.Should().NotBeNull();
        result.Value.Should().Be("anyvalue");
    }

    [Fact]
    public void Equality_WhenSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = PrefixedStringId.NewId("cus_test123");
        var id2 = PrefixedStringId.NewId("cus_test123");

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WhenDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = PrefixedStringId.NewId("cus_test123");
        var id2 = PrefixedStringId.NewId("cus_test456");

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [IdPrefix("cus")]
    public record PrefixedStringId(string Value) : StronglyTypedString<PrefixedStringId>(Value);

    public record UnprefixedStringId(string Value) : StronglyTypedString<UnprefixedStringId>(Value);
}
