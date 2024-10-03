using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class SecretHelper
{
    private static string UserSecretsId => Assembly.GetEntryAssembly()!.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;

    public static void SetSecret(string key, string value)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"user-secrets set {key} {value} --id {UserSecretsId}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
    }

    public static string? GetSecret(string key)
    {
        var config = new ConfigurationBuilder().AddUserSecrets(UserSecretsId).Build();
        return config[key];
    }
}
