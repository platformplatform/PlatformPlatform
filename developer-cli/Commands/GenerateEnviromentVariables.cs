using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class ConfigureDeveloperEnvironment : Command
{
    private static readonly (string ShellName, string ProfileName, string ProfilePath, string UserFolder) ShellInfo =
        AliasRegistration.MacOs.GetShellInfo();

    public ConfigureDeveloperEnvironment() : base(
        "configure-developer-environment",
        "Generates SQL_SERVER_PASSWORD and CERTIFICATE_PASSWORD, adds them to environment variables, and generates a dev certificate."
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
            AnsiConsole.MarkupLine($"Please restart your terminal or run [green]source ~/{ShellInfo.ProfileName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]No changes were made to your environment.[/]");
        }

        return 0;
    }

    private bool CreateSqlServerPasswordIfNotExists()
    {
        var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");

        if (password is not null)
        {
            AnsiConsole.MarkupLine("[green]SQL_SERVER_PASSWORD environment variable already exist.[/]");
            return false;
        }

        password = GenerateRandomPassword(16);
        AddEnvironmentVariable("SQL_SERVER_PASSWORD", password);
        AnsiConsole.MarkupLine("[green]SQL_SERVER_PASSWORD environment variable created.[/]");
        return true;
    }

    public static bool IsValidDeveloperCertificateConfigured()
    {
        var password = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");
        return IsDeveloperCertificateAlreadyConfigured() && IsCertificatePasswordValid(password);
    }

    private static bool EnsureValidCertificateForLocalhostWithKnownPasswordIsConfigured()
    {
        var password = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");

        var isDeveloperCertificateAlreadyConfigured = IsDeveloperCertificateAlreadyConfigured();
        if (isDeveloperCertificateAlreadyConfigured)
        {
            if (IsCertificatePasswordValid(password))
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

        if (password is null)
        {
            password = GenerateRandomPassword(16);
            AddEnvironmentVariable("CERTIFICATE_PASSWORD", password);
        }

        CreateNewSelfSignedDeveloperCertificate(password);

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

        if (output.Contains("A valid certificate was found"))
        {
            return true;
        }

        AnsiConsole.MarkupLine("[yellow]A valid certificate was not found.[/]");
        return false;
    }

    private static bool IsCertificatePasswordValid(string? password)
    {
        if (password is null) return false;
        var shellInfo = ShellInfo;

        var certificateValidationProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments =
                    $"pkcs12 -in {shellInfo.UserFolder}/.aspnet/https/localhost.pfx -passin pass:{password} -nokeys",
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
        while (!deleteCertificateProcess.StandardOutput.EndOfStream)
        {
            var line = deleteCertificateProcess.StandardOutput.ReadLine();
            Console.WriteLine(line);
        }

        deleteCertificateProcess.WaitForExit();
    }

    private static void CreateNewSelfSignedDeveloperCertificate(string password)
    {
        var userFolder = ShellInfo.UserFolder;

        var createCertificateProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"dev-certs https --trust -ep {userFolder}/.aspnet/https/localhost.pfx -p {password}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        createCertificateProcess.Start();
        while (!createCertificateProcess.StandardOutput.EndOfStream)
        {
            var line = createCertificateProcess.StandardOutput.ReadLine();
            Console.WriteLine(line);
        }

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
        if (Environment.GetEnvironmentVariable(variableName) is not null)
        {
            throw new ArgumentException($"Environment variable {variableName} already exists.");
        }

        var fileContent = File.ReadAllText(ShellInfo.ProfilePath);
        if (!fileContent.EndsWith(Environment.NewLine))
        {
            File.AppendAllText(ShellInfo.ProfilePath, Environment.NewLine);
        }

        File.AppendAllText(ShellInfo.ProfilePath, $"export {variableName}='{variableValue}'{Environment.NewLine}");
    }
}