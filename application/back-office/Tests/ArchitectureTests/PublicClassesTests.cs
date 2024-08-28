using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.BackOffice.Core;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using Xunit;

namespace PlatformPlatform.BackOffice.Tests.ArchitectureTests;

public sealed class PublicClassesTests
{
    [Fact]
    public void PublicClassesInApplication_ShouldBeSealed()
    {
        // Act
        var types = Types
            .InAssembly(DependencyConfiguration.Assembly)
            .That().ArePublic()
            .And().AreNotAbstract()
            .And().DoNotHaveName(typeof(Result<>).Name);

        var result = types
            .Should().BeSealed()
            .GetResult();

        // Assert
        var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
    }

    [Fact]
    public void PublicClassesInInfrastructure_ShouldBeSealed()
    {
        // Act
        var types = Types
            .InAssembly(DependencyConfiguration.Assembly)
            .That().ArePublic()
            .And().AreNotAbstract();

        var result = types
            .Should().BeSealed()
            .GetResult();

        // Assert
        var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
    }
}
