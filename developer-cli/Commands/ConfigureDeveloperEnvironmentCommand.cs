using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class ConfigureDeveloperEnvironmentCommand : Command
{
    public const string CommandName = "configure-developer-environment";

    public ConfigureDeveloperEnvironmentCommand() : base(
        CommandName,
        "Generate CERTIFICATE_PASSWORD and SQL_SERVER_PASSWORD, create developer certificate for localhost with known password, and store passwords in environment variables"
    )
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var certificateCreated = EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured();
        var sqlServerPasswordCreated = CreateSqlServerPasswordIfNotExists();

        if (certificateCreated || sqlServerPasswordCreated)
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
        var certificatePassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");

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

    private static bool EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured()
    {
        var certificatePassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");

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
                Environment.Exit(1);
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
        });

        return output.Contains("A valid certificate was found");
    }

    private static bool IsCertificatePasswordValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (!File.Exists(Configuration.LocalhostPfx))
        {
            return false;
        }

        if (Configuration.IsWindows)
        {
            try
            {
                // Try to load the certificate with the provided password
                _ = new X509Certificate2(Configuration.LocalhostPfx, password);
                return true;
            }
            catch (CryptographicException)
            {
                // If a CryptographicException is thrown, the password is invalid
                // Ignore the exception and return false
            }
        }
        else if (Configuration.IsMacOs)
        {
            var arguments = $"pkcs12 -in {Configuration.LocalhostPfx} -passin pass:{password} -nokeys";
            var certificateValidation = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

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
        if (File.Exists(Configuration.LocalhostPfx))
        {
            File.Delete(Configuration.LocalhostPfx);
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
        ProcessHelper.StartProcess($"dotnet dev-certs https --trust -ep {Configuration.LocalhostPfx} -p {password}");
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
        if (Environment.GetEnvironmentVariable(variableName) is not null)
        {
            throw new ArgumentException($"Environment variable {variableName} already exists.");
        }

        if (Configuration.IsWindows)
        {
            var arguments = $"/c setx {variableName} {variableValue}";
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
        }
        else if (Configuration.IsMacOs)
        {
            var fileContent = File.ReadAllText(Configuration.MacOs.GetShellInfo().ProfilePath);
            if (!fileContent.EndsWith(Environment.NewLine))
            {
                File.AppendAllText(Configuration.MacOs.GetShellInfo().ProfilePath, Environment.NewLine);
            }

            File.AppendAllText(Configuration.MacOs.GetShellInfo().ProfilePath,
                $"export {variableName}='{variableValue}'{Environment.NewLine}");
        }
    }
}