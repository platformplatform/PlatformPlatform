using FluentAssertions;
using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.DomainCore.Entities;

public static class EntityTests
{
    public class OperatorOverloadTests
    {
        [Fact]
        public void EqualsOperator_WhenIdIsAreGenerated_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new StronglyTypedIdEntity { Name = "Test" };
            var entity2 = new StronglyTypedIdEntity { Name = "Test" };

            // Act
            var isEqual = entity1 == entity2;

            // Assert
            isEqual.Should().BeFalse();
        }

        [Fact]
        public void EqualsOperator_WhenIdsAreSame_ShouldReturnTrue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var entity1 = new GuidEntity(guid) { Name = "Test" };
            var entity2 = new GuidEntity(guid) { Name = "Different" };

            // Act
            var isEqual = entity1 == entity2;

            // Assert
            isEqual.Should().BeTrue();
        }

        [Fact]
        public void EqualsOperator_WhenIdsAreDifferent_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new StringEntity("id1") { Name = "Test" };
            var entity2 = new StringEntity("id2") { Name = "Test" };

            // Act
            var isEqual = entity1 == entity2;

            // Assert
            isEqual.Should().BeFalse();
        }

        [Fact]
        public void NotOperator_WhenIdsAreEqual_ShouldReturnFalse()
        {
            // Arrange
            var guidId = Guid.NewGuid();
            var entity1 = new GuidEntity(guidId) { Name = "Test" };
            var entity2 = new GuidEntity(guidId) { Name = "Different" };

            // Act
            var isNotEqual = entity1 != entity2;

            // Assert
            isNotEqual.Should().BeFalse();
        }

        [Fact]
        public void NotOperator_WhenIdsAreDifferent_ShouldReturnTrue()
        {
            // Arrange
            var entity1 = new StronglyTypedIdEntity { Name = "Test" };
            var entity2 = new StronglyTypedIdEntity { Name = "Test" };

            // Act
            var isNotEqual = entity1 != entity2;

            // Assert
            isNotEqual.Should().BeTrue();
        }
    }

    public class EqualMethodTests
    {
        [Fact]
        public void Equal_WhenIdIsAreGenerated_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new StronglyTypedIdEntity { Name = "Test" };
            var entity2 = new StronglyTypedIdEntity { Name = "Test" };

            // Act
            var isEqual = entity1.Equals(entity2);

            // Assert
            isEqual.Should().BeFalse();
        }

        [Fact]
        public void Equal_WhenIdsAreEqual_ShouldReturnTrue()
        {
            // Arrange
            const string stringId = "id1";
            var entity1 = new StringEntity(stringId) { Name = "Test" };
            var entity2 = new StringEntity(stringId) { Name = "Different" };

            // Act
            var isEqual = entity1.Equals(entity2);

            // Assert
            isEqual.Should().BeTrue();
        }

        [Fact]
        public void Equal_DifferentIds_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new GuidEntity(Guid.NewGuid()) { Name = "Test" };
            var entity2 = new GuidEntity(Guid.NewGuid()) { Name = "Test" };

            // Act
            var isEqual = entity1.Equals(entity2);

            // Assert
            isEqual.Should().BeFalse();
        }
    }

    public class GetHashCodeTests
    {
        [Fact]
        public void GetHashCode_DifferentIdsSameProperties_ShouldHaveDifferentHashCode()
        {
            // Arrange
            var entity1 = new StronglyTypedIdEntity { Name = "Test" };
            var entity2 = new StronglyTypedIdEntity { Name = "Test" };

            // Act
            var hashCode1 = entity1.GetHashCode();
            var hashCode2 = entity2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2);
        }

        [Fact]
        public void GetHashCode_SameIdsDifferentProperties_ShouldHaveSameHashCode()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity1 = new GuidEntity(id) { Name = "Test" };
            var entity2 = new GuidEntity(id) { Name = "Different" };

            // Act
            var hashCode1 = entity1.GetHashCode();
            var hashCode2 = entity2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2);
        }
    }
}

[UsedImplicitly]
public sealed record StronglyTypedId(long Value) : StronglyTypedLongId<StronglyTypedId>(Value);

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class StronglyTypedIdEntity() : Entity<StronglyTypedId>(StronglyTypedId.NewId())
{
    public required string Name { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class GuidEntity(Guid id) : Entity<Guid>(id)
{
    public required string Name { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class StringEntity(string id) : Entity<string>(id)
{
    public required string Name { get; init; }
}