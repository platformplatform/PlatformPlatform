using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using JetBrains.Annotations;
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
        var isDeveloperCertificateAlreadyConfiguredProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "dev-certs https --check",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        isDeveloperCertificateAlreadyConfiguredProcess.Start();
        var output = isDeveloperCertificateAlreadyConfiguredProcess.StandardOutput.ReadToEnd();
        isDeveloperCertificateAlreadyConfiguredProcess.WaitForExit();

        return output.Contains("A valid certificate was found");
    }

    private static bool IsCertificatePasswordValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (!File.Exists(Environment.MacOs.LocalhostPfx))
        {
            return false;
        }

        var certificateValidationProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = $"pkcs12 -in {Environment.MacOs.LocalhostPfx} -passin pass:{password} -nokeys",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        certificateValidationProcess.Start();
        var certificateValidation = certificateValidationProcess.StandardOutput.ReadToEnd();
        certificateValidationProcess.WaitForExit();

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
        var deleteCertificateProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "dev-certs https --clean",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        deleteCertificateProcess.Start();

        deleteCertificateProcess.WaitForExit();
    }

    private static void CreateNewSelfSignedDeveloperCertificate(string password)
    {
        var localhostPfx = Environment.IsWindows ? Environment.Windows.LocalhostPfx : Environment.MacOs.LocalhostPfx;

        var createCertificateProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"dev-certs https --trust -ep {localhostPfx} -p {password}"
            }
        };

        createCertificateProcess.Start();
        createCertificateProcess.WaitForExit();
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
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c setx {variableName} {variableValue}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
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