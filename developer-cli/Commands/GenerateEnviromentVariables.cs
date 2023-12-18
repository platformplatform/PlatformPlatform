using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class ConfigureDeveloperEnvironment : Command
{
    public const string CommandName = "configure-developer-environment";

    public ConfigureDeveloperEnvironment() : base(
        CommandName,
        "Generates a developer certificate for localhost. Generates CERTIFICATE_PASSWORD SQL_SERVER_PASSWORD saves them in environment variables."
    )
    {
        Handler = CommandHandler.Create(new Func<int>(Execute));
    }

    private int Execute()
    {
        var certificateCreated = EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured();
        var passwordCreated = CreateSqlServerPasswordIfNotExists();

        if (passwordCreated || certificateCreated)
        {
            AnsiConsole.MarkupLine(
                $"Please restart your terminal or run [green]source ~/{Environment.MacOs.ShellInfo.ProfileName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]No changes were made to your environment.[/]");
        }

        return 0;
    }

    private bool CreateSqlServerPasswordIfNotExists()
    {
        var certificatePassword = System.Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");

        if (certificatePassword is not null)
        {
            AnsiConsole.MarkupLine("[green]SQL_SERVER_PASSWORD environment variable already exist.[/]");
            return false;
        }

        certificatePassword = GenerateRandomPassword(16);
        AddEnvironmentVariable("SQL_SERVER_PASSWORD", certificatePassword);
        AnsiConsole.MarkupLine("[green]SQL_SERVER_PASSWORD environment variable created.[/]");
        return true;
    }

    public static bool IsDeveloperCertificateConfigured()
    {
        if (!IsDeveloperCertificateAlreadyConfigured())
        {
            AnsiConsole.MarkupLine("[yellow]Developer certificate is not configured.[/]");
            return false;
        }

        var certificatePassword = System.Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");
        if (certificatePassword is null)
        {
            AnsiConsole.MarkupLine("[yellow]CERTIFICATE_PASSWORD environment variable is not set.[/]");
            return false;
        }

        if (!IsCertificatePasswordValid(certificatePassword))
        {
            AnsiConsole.MarkupLine("[yellow]A valid certificate password is not configured.[/]");
            return false;
        }

        return true;
    }

    private static bool EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured()
    {
        var certificatePassword = System.Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");

        var isDeveloperCertificateAlreadyConfigured = IsDeveloperCertificateAlreadyConfigured();
        if (isDeveloperCertificateAlreadyConfigured)
        {
            if (IsCertificatePasswordValid(certificatePassword))
            {
                AnsiConsole.MarkupLine("[green]The existing certificate is valid and password is valid.[/]");
                return false;
            }

            if (!AnsiConsole.Confirm(
                    "Existing certificate exists, but the password is unknown. A new developer certificate will be created and the password will be stored in an environment variable."))
            {
                AnsiConsole.MarkupLine(
                    "[red]Debugging PlatformPlatform will not work as the password for the Localhost certificate is unknown.[/]");
                System.Environment.Exit(1);
            }

            CleanExistingCertificate();
        }

        if (certificatePassword is null)
        {
            certificatePassword = GenerateRandomPassword(16);
            AddEnvironmentVariable("CERTIFICATE_PASSWORD", certificatePassword);
        }

        CreateNewSelfSignedDeveloperCertificate(certificatePassword);

        return true;
    }

    private static bool IsDeveloperCertificateAlreadyConfigured()
    {
        var output = ProcessHelper.StartProcess(
            "dotnet",
            "dev-certs https --check",
            redirectStandardOutput: true,
            createNoWindow: true,
            printCommand: false
        );

        return output.Contains("A valid certificate was found");
    }

    private static bool IsCertificatePasswordValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (!File.Exists(Environment.MacOs.LocalhostPfx))
        {
            return false;
        }

        var certificateValidation = ProcessHelper.StartProcess(
            "openssl",
            $"pkcs12 -in {Environment.MacOs.LocalhostPfx} -passin pass:{password} -nokeys",
            redirectStandardOutput: true,
            createNoWindow: true,
            printCommand: false
        );

        if (certificateValidation.Contains("--BEGIN CERTIFICATE--"))
        {
            return true;
        }

        AnsiConsole.MarkupLine("[red]The password for the certificate is invalid.[/]");
        return false;
    }

    private static void CleanExistingCertificate()
    {
        File.Delete(Environment.MacOs.LocalhostPfx);
        ProcessHelper.StartProcess(
            "dotnet",
            "dev-certs https --clean",
            redirectStandardOutput: true,
            createNoWindow: true,
            printCommand: true
        );
    }

    private static void CreateNewSelfSignedDeveloperCertificate(string password)
    {
        var localhostPfx = Environment.IsWindows ? Environment.Windows.LocalhostPfx : Environment.MacOs.LocalhostPfx;

        ProcessHelper.StartProcess(
            "dotnet",
            $"dev-certs https --trust -ep {localhostPfx} -p {password}"
        );
    }

    private static string GenerateRandomPassword(int passwordLength)
    {
        // Please note that this is not a cryptographically secure password generator
        const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789_-!#&%@$?";
        var chars = new char[passwordLength];
        var random = new Random();

        for (var i = 0; i < passwordLength; i++)
        {
            chars[i] = allowedChars[random.Next(0, allowedChars.Length)];
        }

        return new string(chars);
    }

    private static void AddEnvironmentVariable(string variableName, string variableValue)
    {
        if (System.Environment.GetEnvironmentVariable(variableName) is not null)
        {
            throw new ArgumentException($"Environment variable {variableName} already exists.");
        }

        if (Environment.IsWindows)
        {
            ProcessHelper.StartProcess(
                "cmd.exe",
                $"/c setx {variableName} {variableValue}",
                redirectStandardOutput: true,
                createNoWindow: true,
                printCommand: false
            );
        }
        else
        {
            var fileContent = File.ReadAllText(Environment.MacOs.ShellInfo.ProfilePath);
            if (!fileContent.EndsWith(System.Environment.NewLine))
            {
                File.AppendAllText(Environment.MacOs.ShellInfo.ProfilePath, System.Environment.NewLine);
            }

            File.AppendAllText(Environment.MacOs.ShellInfo.ProfilePath,
                $"export {variableName}='{variableValue}'{System.Environment.NewLine}");
        }
    }
}