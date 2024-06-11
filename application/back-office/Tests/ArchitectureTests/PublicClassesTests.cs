using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.BackOffice.Application;
using PlatformPlatform.BackOffice.Domain;
using PlatformPlatform.BackOffice.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using Xunit;

namespace PlatformPlatform.BackOffice.Tests.ArchitectureTests;

public sealed class PublicClassesTests
{
    [Fact]
    public void PublicClassesInDomain_ShouldBeSealed()
    {
        // Act
        var result = Types
            .InAssembly(DomainConfiguration.Assembly)
            .That().ArePublic()
            .And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();
        
        // Assert
        var nonSealedTypes = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following are not sealed: {nonSealedTypes}");
    }
    
    [Fact]
    public void PublicClassesInApplication_ShouldBeSealed()
    {
        // Act
        var types = Types
            .InAssembly(ApplicationConfiguration.Assembly)
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
            .InAssembly(InfrastructureConfiguration.Assembly)
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
