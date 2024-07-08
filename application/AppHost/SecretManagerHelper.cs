using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace AppHost;

public static class SecretManagerHelper
{
    private static string UserSecretsId =>
        Assembly.GetEntryAssembly()!.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;

    public static IResourceBuilder<ParameterResource> CreateStablePassword(
        this IDistributedApplicationBuilder builder,
        string secretName
    )
    {
        var config = new ConfigurationBuilder().AddUserSecrets(UserSecretsId).Build();

        var password = config[secretName];

        if (string.IsNullOrEmpty(password))
        {
            var passwordGenerator = new GenerateParameterDefault
            {
                MinLower = 5, MinUpper = 5, MinNumeric = 3, MinSpecial = 3
            };
            password = passwordGenerator.GetDefaultValue();
            SavePasswordToUserSecrets(secretName, password);
        }

        return builder.CreateResourceBuilder(new ParameterResource(secretName, _ => password, true));
    }

    private static void SavePasswordToUserSecrets(string key, string value)
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
