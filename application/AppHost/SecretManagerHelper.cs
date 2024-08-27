using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AppHost;

public static class SecretManagerHelper
{
    private static readonly IConfigurationRoot ConfigurationRoot = new ConfigurationBuilder().AddUserSecrets(UserSecretsId).Build();

    private static string UserSecretsId => Assembly.GetEntryAssembly()!.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;

    public static IResourceBuilder<ParameterResource> CreateStablePassword(
        this IDistributedApplicationBuilder builder,
        string secretName
    )
    {
        var password = ConfigurationRoot[secretName];

        if (string.IsNullOrEmpty(password))
        {
            var passwordGenerator = new GenerateParameterDefault
            {
                MinLower = 5, MinUpper = 5, MinNumeric = 3, MinSpecial = 3
            };
            password = passwordGenerator.GetDefaultValue();
            SaveSecrectToDotNetUserSecrets(secretName, password);
        }

        return builder.CreateResourceBuilder(new ParameterResource(secretName, _ => password, true));
    }

    public static void GenerateAuthenticationTokenSigningKey(string secretName)
    {
        if (string.IsNullOrEmpty(ConfigurationRoot[secretName]))
        {
            var key = new byte[64]; // 512-bit key
            RandomNumberGenerator.Fill(key);
            var base64Key = Convert.ToBase64String(key);
            SaveSecrectToDotNetUserSecrets(secretName, base64Key);
        }
    }

    private static void SaveSecrectToDotNetUserSecrets(string key, string value)
    {
        var args = $"user-secrets set {key} {value} --id {UserSecretsId}";
        var startInfo = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
    }
}
