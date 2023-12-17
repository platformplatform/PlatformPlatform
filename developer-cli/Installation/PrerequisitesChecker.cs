using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Commands;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class PrerequisitesChecker
{
    public static void EnsurePrerequisitesAreMeet()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[red]Only Windows and macOS are currently supported.[/]");
            Environment.Exit(1);
        }

        var checkAzureCli = CheckCommandLineTool("az", new Version(2, 55));
        var checkBun = CheckCommandLineTool("bun", new Version(1, 0));
        var docker = CheckCommandLineTool("docker", new Version(24, 0));

        if (!checkAzureCli || !checkBun || !docker)
        {
            Environment.Exit(1);
        }

        var sqlPasswordConfigured = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD") is not null;
        if (!sqlPasswordConfigured)
        {
            AnsiConsole.MarkupLine("[yellow]SQL_SERVER_PASSWORD environment variable is not set.[/]");
        }

        var isValidDeveloperCertificateConfigured =
            ConfigureDeveloperEnvironment.IsValidDeveloperCertificateConfigured();

        if (!sqlPasswordConfigured || !isValidDeveloperCertificateConfigured)
        {
            AnsiConsole.MarkupLine(
                "[yellow]Please run ´pp configure-developer-environment´ to configure your environment.[/]");
        }
    }

    private static bool CheckCommandLineTool(string command, Version minVersion)
    {
        // Check if the command line tool is installed
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var checkCommand = isWindows ? "where" : "which";

        var checkProcess = Process.Start(new ProcessStartInfo
        {
            FileName = checkCommand,
            Arguments = command,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var checkOutput = checkProcess!.StandardOutput.ReadToEnd();
        if (string.IsNullOrWhiteSpace(checkOutput))
        {
            AnsiConsole.MarkupLine($"[red]´{command}´ is not installed. This tool is required by PlatformPlatform.[/]");
            return false;
        }

        // Get the version of the command line tool
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/c {command} --version"
                : $"-c \"{command} --version\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var output = process!.StandardOutput.ReadToEnd();

        var versionRegex = new Regex(@"\d+\.\d+\.\d+(\.\d+)?");
        var match = versionRegex.Match(output);

        if (match.Success)
        {
            var version = Version.Parse(match.Value);
            if (version >= minVersion) return true;
            AnsiConsole.MarkupLine(
                $"[red]Please update ´{command}´ from version {minVersion} to {version} or later.[/]");
            return false;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Command ´{command}´ is installed but version could not be determined. Please update the CLI to check for correct version.[/]");
        return false;
    }
}