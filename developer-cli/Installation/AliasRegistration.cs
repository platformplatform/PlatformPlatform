using System.Reflection;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class AliasRegistration
{
    public static readonly string AliasName = Assembly.GetExecutingAssembly().GetName().Name!;
}