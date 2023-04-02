using System.Reflection;

namespace PlatformPlatform.Domain;

public static class DomainAssembly
{
    public static readonly Assembly Assembly = typeof(DomainAssembly).Assembly;

    public static readonly string Name = Assembly.GetName().Name!;
}