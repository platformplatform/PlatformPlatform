using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ArchitectureTests;

public class PublicClassesTests
{
    [Fact]
    public void PublicClassesInDomain_Should_BeSealed()
    {
        // Act
        var result = Types
            .InAssembly(DomainConfiguration.Assembly)
            .That()
            .ArePublic()
            .And().AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void PublicClassesInApplication_Should_BeSealed()
    {
        // Act
        var result = Types
            .InAssembly(ApplicationConfiguration.Assembly)
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