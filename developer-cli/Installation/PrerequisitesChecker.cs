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
        var aspire = CheckDotnetWorkload("aspire", """aspire\s*8\.0\.0-preview"""); // aspire 8.0.0-preview.1.23557

        if (!checkAzureCli || !checkBun || !docker || !aspire)
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
            AnsiConsole.MarkupLine(
                $"[red]The [bold]{command}[/] is not installed. Please see the README file for guidance.[/]");
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
                $"[red]Please update [bold]{command}[/] from version [bold]{minVersion}[/] to [bold]{version}[/] or later.[/]");
            return false;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Command [bold]{command}[/] is installed but version could not be determined. Please update the CLI to check for correct version.[/]");
        return false;
    }

    private static bool CheckDotnetWorkload(string workloadName, string workloadRegex)
    {
        var dotnetWorkloadProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "workload list",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var output = dotnetWorkloadProcess!.StandardOutput.ReadToEnd();
        if (!output.Contains(workloadName))
        {
            AnsiConsole.MarkupLine(
                $"[red].NET [bold]{workloadName}[/] workload is not installed. Please run '[bold]dotnet workload install {workloadName}[/]' to install.[/]");
            AnsiConsole.MarkupLine("[red][/]");
            return false;
        }

        /*
           The output is on the form:

           Installed Workload Id      Manifest Version                     Installation Source
           -----------------------------------------------------------------------------------
           aspire                     8.0.0-preview.1.23557.2/8.0.100      SDK 8.0.100

           Use `dotnet workload search` to find additional workloads to install.
         */
        var regex = new Regex(workloadRegex);
        var match = regex.Match(output);

        if (match.Success)
        {
            return true;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Dotnet [bold]{workloadName}[/] workload is installed but not in the expected version. Please upgrade the workflow or update the CLI to check for correct version.[/]");
        return false;
    }
}