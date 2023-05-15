using FluentAssertions;
using PlatformPlatform.Foundation.Domain;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Domain;

public class EntityEqualityComparerTests
{
    private readonly EntityEqualityComparer<Guid> _comparer = new();

    [Fact]
    public void Equals_WithSameEntity_ShouldReturnTrue()
    {
        // Arrange
        var entity = new GuidEntity(Guid.NewGuid()) {Name = "Test"};

        // Act
        var isEqual = _comparer.Equals(entity, entity);

        // Assert
        isEqual.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameIdDifferentProperty_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new GuidEntity(id) {Name = "Test"};
        var entity2 = new GuidEntity(id) {Name = "Different"};

        // Act
        var isEqual = _comparer.Equals(entity1, entity2);

        // Assert
        isEqual.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentIds_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new GuidEntity(Guid.NewGuid()) {Name = "Test"};
        var entity2 = new GuidEntity(Guid.NewGuid()) {Name = "Test"};

        // Act
        var isEqual = _comparer.Equals(entity1, entity2);

        // Assert
        isEqual.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new GuidEntity(id) {Name = "Test"};
        var entity2 = new GuidEntity(id) {Name = "Different"};

        // Act
        var hashCode1 = _comparer.GetHashCode(entity1);
        var hashCode2 = _comparer.GetHashCode(entity2);

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentIds_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var entity1 = new GuidEntity(Guid.NewGuid()) {Name = "Test"};
        var entity2 = new GuidEntity(Guid.NewGuid()) {Name = "Test"};

        // Act
        var hashCode1 = _comparer.GetHashCode(entity1);
        var hashCode2 = _comparer.GetHashCode(entity2);

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }
}