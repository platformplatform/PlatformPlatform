using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Commands;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class PrerequisitesChecker
{
    public static void EnsurePrerequisitesAreMeet(string[] args)
    {
        var checkAzureCli = CheckCommandLineTool("az", new Version(2, 55));
        var checkBun = CheckCommandLineTool("bun", new Version(1, 0));
        var docker = CheckCommandLineTool("docker", new Version(24, 0));
        var aspire = CheckDotnetWorkload("aspire", """aspire\s*8\.0\.0-preview.2"""); // aspire 8.0.0-preview.2.23619.3

        if (!checkAzureCli || !checkBun || !docker || !aspire)
        {
            System.Environment.Exit(1);
        }

        if (args.Contains(ConfigureDeveloperEnvironment.CommandName))
        {
            // If we are configuring the environment we don't need to check if configuring the environment is needed
            return;
        }

        // If Environment variables are set but not sourced exit hard with a message
        EnsureEnvironmentVariableAreSourced("SQL_SERVER_PASSWORD");
        EnsureEnvironmentVariableAreSourced("CERTIFICATE_PASSWORD");

        var sqlPasswordConfigured = System.Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD") is not null;
        if (!sqlPasswordConfigured)
        {
            AnsiConsole.MarkupLine("[yellow]SQL_SERVER_PASSWORD environment variable is not set.[/]");
        }

        var hasValidDeveloperCertificate = ConfigureDeveloperEnvironment.HasValidDeveloperCertificate();

        if (!sqlPasswordConfigured || !hasValidDeveloperCertificate)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Please run 'pp {ConfigureDeveloperEnvironment.CommandName}' to configure your environment.[/]");
            AnsiConsole.WriteLine();
        }
    }

    private static bool CheckCommandLineTool(string command, Version minVersion)
    {
        // Check if the command line tool is installed
        var checkOutput = ProcessHelper.StartProcess(
            Environment.IsWindows ? "where" : "which",
            command,
            redirectStandardOutput: true,
            printCommand: false
        );

        if (string.IsNullOrWhiteSpace(checkOutput))
        {
            AnsiConsole.MarkupLine(
                $"[red]The '[bold]{command}[/]' is not installed. Please see the README file for guidance.[/]");
            return false;
        }

        // Get the version of the command line tool
        var output = ProcessHelper.StartProcess(
            Environment.IsWindows ? "cmd.exe" : "/bin/bash",
            Environment.IsWindows ? $"/c {command} --version" : $"-c \"{command} --version\"",
            redirectStandardOutput: true,
            printCommand: false
        );

        var versionRegex = new Regex(@"\d+\.\d+\.\d+(\.\d+)?");
        var match = versionRegex.Match(output);
        if (match.Success)
        {
            var version = Version.Parse(match.Value);
            if (version >= minVersion) return true;
            AnsiConsole.MarkupLine(
                $"[red]Please update '[bold]{command}[/]' from version [bold]{version}[/] to [bold]{minVersion}[/] or later.[/]");
            return false;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Command '[bold]{command}[/]' is installed but version could not be determined. Please update the CLI to check for correct version.[/]");
        return false;
    }

    private static bool CheckDotnetWorkload(string workloadName, string workloadRegex)
    {
        var output = ProcessHelper.StartProcess(
            "dotnet",
            "workload list",
            redirectStandardOutput: true,
            printCommand: false
        );

        if (!output.Contains(workloadName))
        {
            AnsiConsole.MarkupLine($"[red].NET '[bold]{workloadName}[/]' workload is not installed.[/]");
            AnsiConsole.MarkupLine(
                $"[red]Please run '[bold]dotnet workload update[/]' and then '[bold]dotnet workload install {workloadName}[/]'.");
            return false;
        }

        /*
           The output is on the form:

           Installed Workload Id      Manifest Version                     Installation Source
           -----------------------------------------------------------------------------------
           aspire                     8.0.0-preview.2.23619.3/8.0.100      SDK 8.0.100

           Use `dotnet workload search` to find additional workloads to install.
         */
        var regex = new Regex(workloadRegex);
        var match = regex.Match(output);
        if (!match.Success)
        {
            // If the version could not be determined please change the logic here to check for the correct version
            AnsiConsole.MarkupLine(
                $"[red].NET '[bold]{workloadName}[/]' workload is installed but not in the expected version.[/]");
            AnsiConsole.MarkupLine("[red]Please run '[bold]dotnet workload update[/]'.[/]");
        }

        return match.Success;
    }

    private static void EnsureEnvironmentVariableAreSourced(string variableName)
    {
        if (System.Environment.GetEnvironmentVariable(variableName) is not null) return;

        if (Environment.IsWindows) return;

        var fileContent = File.ReadAllText(Environment.MacOs.GetShellInfo().ProfilePath);
        if (!fileContent.Contains($"export {variableName}")) return;

        AnsiConsole.MarkupLine(
            $"[red]'{variableName}' is configured but not available. Please run '[bold]source ~/{Environment.MacOs.GetShellInfo().ProfileName}[/]'[/]");
        System.Environment.Exit(0);
    }
}