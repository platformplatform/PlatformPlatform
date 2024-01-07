using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        "Generate CERTIFICATE_PASSWORD and SQL_SERVER_PASSWORD, create developer certificate for localhost with known password, and store passwords in environment variables"
    )
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var certificateCreated = EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured();
        var passwordCreated = CreateSqlServerPasswordIfNotExists();

        if (passwordCreated || certificateCreated)
        {
            AnsiConsole.MarkupLine("[green]Please restart your terminal.[/]");
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

    public static bool HasValidDeveloperCertificate()
    {
        if (!IsDeveloperCertificateInstalled())
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

        var isDeveloperCertificateInstalled = IsDeveloperCertificateInstalled();
        if (isDeveloperCertificateInstalled)
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

    private static bool IsDeveloperCertificateInstalled()
    {
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "dev-certs https --check",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }, printCommand: false);

        return output.Contains("A valid certificate was found");
    }

    private static bool IsCertificatePasswordValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (!File.Exists(Environment.LocalhostPfx))
        {
            return false;
        }

        if (Environment.IsWindows)
        {
            try
            {
                // Try to load the certificate with the provided password
                _ = new X509Certificate2(Environment.LocalhostPfx, password);
                return true;
            }
            catch (CryptographicException)
            {
                // If a CryptographicException is thrown, the password is invalid
                // Ignore the exception and return false
            }
        }
        else if (Environment.IsMacOs)
        {
            var arguments = $"pkcs12 -in {Environment.LocalhostPfx} -passin pass:{password} -nokeys";
            var certificateValidation = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }, printCommand: false);

            if (certificateValidation.Contains("--BEGIN CERTIFICATE--"))
            {
                return true;
            }
        }

        AnsiConsole.MarkupLine("[red]The password for the certificate is invalid.[/]");
        return false;
    }

    private static void CleanExistingCertificate()
    {
        if (File.Exists(Environment.LocalhostPfx))
        {
            File.Delete(Environment.LocalhostPfx);
        }

        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "dev-certs https --clean",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });
    }

    private static void CreateNewSelfSignedDeveloperCertificate(string password)
    {
        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"dev-certs https --trust -ep {Environment.LocalhostPfx} -p {password}"
        });
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
            var arguments = $"/c setx {variableName} {variableValue}";
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }, printCommand: false);
        }
        else if (Environment.IsMacOs)
        {
            var fileContent = File.ReadAllText(Environment.MacOs.GetShellInfo().ProfilePath);
            if (!fileContent.EndsWith(System.Environment.NewLine))
            {
                File.AppendAllText(Environment.MacOs.GetShellInfo().ProfilePath, System.Environment.NewLine);
            }

            File.AppendAllText(Environment.MacOs.GetShellInfo().ProfilePath,
                $"export {variableName}='{variableValue}'{System.Environment.NewLine}");
        }
    }
}