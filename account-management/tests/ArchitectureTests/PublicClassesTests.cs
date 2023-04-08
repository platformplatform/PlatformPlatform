using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Domain;

namespace PlatformPlatform.AccountManagement.Tests.ArchitectureTests;

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
            .And().AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}