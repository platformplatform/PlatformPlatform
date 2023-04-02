using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Domain;

namespace PlatformPlatform.AccountManagement.ArchitectureTests;

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