using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.Foundation.Application;
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
        var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
    }

    [Fact]
    public void PublicClassesInApplication_Should_BeSealed()
    {
        // Act
        var types = Types
            .InAssembly(ApplicationConfiguration.Assembly)
            .That()
            .ArePublic()
            .And().AreNotAbstract()
            .And().DoNotHaveName(typeof(CommandResult<>).Name)
            .And().DoNotHaveName(typeof(QueryResult<>).Name);

        var result = types
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
    }
}