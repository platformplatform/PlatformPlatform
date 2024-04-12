using FluentAssertions;
using PlatformPlatform.SharedKernel.DomainCore.Identity;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ArchitectureTests;

public class IdPrefixForAllStronglyTypedUlidTests
{
    [Fact]
    public void AllStronglyTypedUlidTests_ShouldContainTheIdPrefixAttribute()
    {
        // Arrange
        // Act
        var allStronglyTypedUlidObjects = Assembly.GetAssembly(typeof(User))!
            .GetTypes()
            .Where(t => t.BaseType is { IsGenericType: true } &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(StronglyTypedUlid<>)).ToList();

        // Assert
        allStronglyTypedUlidObjects.Should()
            .AllSatisfy(o => o.GetCustomAttribute<IdPrefixAttribute>().Should().NotBeNull());
    }
}