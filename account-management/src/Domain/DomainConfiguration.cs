using System.Reflection;

namespace PlatformPlatform.AccountManagement.Domain;

/// <summary>
///     The DomainConfiguration class is used to register services used by the domain layer
///     with the dependency injection container.
/// </summary>
public static class DomainConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();
}