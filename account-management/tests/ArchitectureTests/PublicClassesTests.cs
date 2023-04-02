using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.Domain;

namespace PlatformPlatform.ArchitectureTests;

public class PublicClassesTests
{
    [Fact]
    public void PublicClassesInDomain_Should_BeSealed()
    {
        // Act
        var result = Types
            .InAssembly(DomainAssembly.Assembly)
            .That()
            .ArePublic()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}