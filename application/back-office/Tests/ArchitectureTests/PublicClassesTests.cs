using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.SharedKernel.Cqrs;
using Xunit;

namespace PlatformPlatform.BackOffice.Tests.ArchitectureTests;

public sealed class PublicClassesTests
{
    [Fact]
    public void PublicClassesInCore_ShouldBeSealed()
    {
        // Act
        var types = Types
            .InAssembly(Configuration.Assembly)
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
}
