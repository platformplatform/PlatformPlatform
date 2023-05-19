using FluentAssertions;
using PlatformPlatform.Foundation.DddCore;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Domain;

public class IdGeneratorTests
{
    [Fact]
    public void NewId_WhenGeneratingIds_IdsShouldBeUnique()
    {
        // Arrange
        const int idCount = 1000;
        var generatedIds = new HashSet<long>();

        // Act
        for (var i = 0; i < idCount; i++)
        {
            generatedIds.Add(IdGenerator.NewId());
        }

        // Assert
        generatedIds.Count.Should().Be(idCount);
    }

    [Fact]
    public void NewId_WhenGeneratingIds_IdsShouldBeIncreasing()
    {
        // Arrange
        const int idCount = 1000;
        var previousId = 0L;

        // Act & Assert
        for (var i = 0; i < idCount; i++)
        {
            var currentId = IdGenerator.NewId();
            currentId.Should().BeGreaterThan(previousId);
            previousId = currentId;
        }
    }
}