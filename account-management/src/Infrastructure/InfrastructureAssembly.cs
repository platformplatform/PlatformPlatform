using System.Reflection;

namespace PlatformPlatform.Infrastructure;

public static class InfrastructureAssembly
{
    public static readonly Assembly Assembly = typeof(InfrastructureAssembly).Assembly;

    public static readonly string Name = Assembly.GetName().Name!;
}