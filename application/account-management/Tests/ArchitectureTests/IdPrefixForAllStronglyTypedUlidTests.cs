using FluentAssertions;
using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.SharedKernel.DomainCore.Identity;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ArchitectureTests;

public class IdPrefixForAllStronglyTypedUlidTests
{
    [Fact]
    public void StronglyTypedUlidsInDomain_ShouldHaveIdPrefixAttribute()
    {
        // Act
        var result = Types
            .InAssembly(DomainConfiguration.Assembly)
            .That().Inherit(typeof(StronglyTypedUlid<>))
            .Should().HaveCustomAttribute(typeof(IdPrefixAttribute))
            .GetResult();
        
        // Assert
        var idsWithoutPrefix = string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? Array.Empty<string>());
        result.IsSuccessful.Should().BeTrue($"The following strongly typed IDs does not have an IdPrefixAttribute: {idsWithoutPrefix}");
    }
    
    [Fact]
    public void StronglyTypedUlidsInDomain_ShouldHaveValidIdPrefix()
    {
        // Arrange
        var stronglyTypedUlidIds = Types
            .InAssembly(DomainConfiguration.Assembly)
            .That().Inherit(typeof(StronglyTypedUlid<>))
            .GetTypes();
        
        // Assert
        foreach (var stronglyTypedId in stronglyTypedUlidIds)
        {
            var newId = stronglyTypedId.BaseType?.GetMethod("NewId")?.Invoke(null, null);
            
            // Ids must follow the pattern: {prefix}_{ULID} where prefix is lowercase and ULID is uppercase
            newId?.ToString().Should().MatchRegex("^[a-z0-9]+_[A-Z0-9]{26}$");
        }
    }
}
