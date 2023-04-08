using NetArchTest.Rules;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;

namespace PlatformPlatform.AccountManagement.Tests.ArchitectureTests;

public class DependencyTests
{
    [Fact]
    public void WebApi_ShouldNot_HaveDependencyOnInfrastructure()
    {
        // Act
        var result = Types
            .InAssembly(WebApiConfiguration.Assembly)
            .That()
            .DoNotHaveNameMatching("Program") // The Program class are allowed to register infrastructure services
            .ShouldNot()
            .HaveDependencyOn(InfrastructureConfiguration.Assembly.GetName().Name!)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNot_HaveDependencyOnInfrastructureAndWebApi()
    {
        // Arrange
        string[] otherAssemblies =
        {
            InfrastructureConfiguration.Assembly.GetName().Name!,
            WebApiConfiguration.Assembly.GetName().Name!
        };

        // Act
        var result = Types
            .InAssembly(ApplicationConfiguration.Assembly)
            .ShouldNot()
            .HaveDependencyOnAll(otherAssemblies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnOtherProjects()
    {
        // Arrange
        var otherAssemblies = new[]
        {
            ApplicationConfiguration.Assembly.GetName().Name!,
            InfrastructureConfiguration.Assembly.GetName().Name!,
            WebApiConfiguration.Assembly.GetName().Name!
        };

        // Act
        var result = Types
            .InAssembly(DomainConfiguration.Assembly)
            .ShouldNot()
            .HaveDependencyOnAll(otherAssemblies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}