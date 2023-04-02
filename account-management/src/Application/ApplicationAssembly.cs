using System.Reflection;

namespace PlatformPlatform.Application;

public static class ApplicationAssembly
{
    public static readonly Assembly Assembly = typeof(ApplicationAssembly).Assembly;

    public static readonly string Name = Assembly.GetName().Name!;
}